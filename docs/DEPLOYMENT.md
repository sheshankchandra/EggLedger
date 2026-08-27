# Azure Deployment Runbook

Manual deployment of EggLedger to Azure using the `az` CLI. Target architecture:

| Component | Azure service |
| --- | --- |
| Vue SPA | Static Web Apps (`eggledger.sshnk.com`) |
| .NET API | Container Apps, scale-to-zero (`api.sshnk.com`) |
| Database | PostgreSQL Flexible Server (Burstable B1ms) |
| API image | Container Registry (managed-identity pull) |
| Secrets | Key Vault (referenced by managed identity) |
| Observability | Application Insights (OpenTelemetry) |

**Live:** frontend at <https://eggledger.sshnk.com>, API at <https://api.sshnk.com>.

Secrets (JWT key, DB connection, Google OAuth, App Insights) live in Key Vault and are pulled
by the Container App's managed identity — never committed. See `docs/SECRETS.md`.

> Status: complete. Sections 1–8 cover the initial deploy; sections 9–13 cover the
> Phase 4 hardening (observability, managed identity, Key Vault, custom domain, and
> the Google OAuth topology).

## Prerequisites

```powershell
winget install Microsoft.AzureCLI            # Azure CLI
az login --tenant <tenant-id>                # sign in to the tenant that holds the subscription
az account show -o table                     # confirm the active subscription
```

Docker Desktop and the .NET 10 runtime are also required locally.

## 1. Resource group

A single resource group holds everything, so cleanup is one command
(`az group delete --name rg-eggledger-prod`).

```powershell
az group create --name rg-eggledger-prod --location centralindia -o table
```

## 2. PostgreSQL Flexible Server

Burstable B1ms is the cheapest tier. Public endpoint locked to a firewall
allow-list (your IP for migrations + Azure services for the app).

```powershell
# Strong password (alphanumeric is safe for connection strings). Save it in a
# password manager; it becomes a Container Apps secret later, never committed.
$pgPass = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 20 | % {[char]$_})

# Create the server. --public-access <ip> turns the public endpoint ON and adds a
# firewall rule for that IP in one step. (--public-access None would disable the
# endpoint entirely, which blocks firewall rules.) Server name is globally unique.
$myIp = (Invoke-RestMethod https://api.ipify.org)
az postgres flexible-server create `
  --resource-group rg-eggledger-prod --name eggledger-pg-prod1 `
  --location centralindia `
  --tier Burstable --sku-name Standard_B1ms `
  --storage-size 32 --version 16 `
  --admin-user eggledgeradmin --admin-password "$pgPass" `
  --public-access $myIp -o table

# Application database (note: db create uses --name, not --database-name)
az postgres flexible-server db create `
  --resource-group rg-eggledger-prod --server-name eggledger-pg-prod1 --name eggledgerdb -o table

# Allow other Azure services (Container Apps). Note the flags: firewall-rule uses
# --server-name for the server and --name for the rule.
az postgres flexible-server firewall-rule create `
  --resource-group rg-eggledger-prod --server-name eggledger-pg-prod1 `
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o table

# Server hostname for the connection string
az postgres flexible-server show `
  --resource-group rg-eggledger-prod --name eggledger-pg-prod1 --query "fullyQualifiedDomainName" -o tsv
```

**CLI flag gotchas** (they differ per sub-command): `flexible-server show`/`create`
use `--name` for the server; `db create` and `firewall-rule create` use
`--server-name` for the server and `--name` for the resource.

## 3. Apply EF migrations to Azure Postgres

Azure Postgres requires SSL, so the connection string needs
`SSL Mode=Require;Trust Server Certificate=true`. `--connection` overrides the
DbContext's configured string for this operation only.

```powershell
dotnet ef database update `
  --project EggLedger.Data --startup-project EggLedger.API `
  --connection "Host=eggledger-pg-prod1.postgres.database.azure.com;Port=5432;Database=eggledgerdb;Username=eggledgeradmin;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
```

> `dotnet ef` runs the app's startup, which also triggers the in-app `Ef_Migrate`
> migrator against the *configured* (dev) connection. To force Azure and avoid a
> double-migrate, override the connection via env var and disable the in-app one:
>
> ```powershell
> $env:ConnectionStrings__DefaultConnection = "<azure connection string>"
> $env:Ef_Migrate = "false"
> dotnet ef database update --project EggLedger.Data --startup-project EggLedger.API
> Remove-Item Env:ConnectionStrings__DefaultConnection, Env:Ef_Migrate
> ```

## 4. Container Registry + API image

Container settings (repository, base image, amd64 RID, port 8080) live in
`EggLedger.API.csproj`, so the publish command only supplies the registry and tag.

```powershell
az acr create --resource-group rg-eggledger-prod --name eggledgeracr1 --sku Basic -o table
az acr login --name eggledgeracr1

# Build the image with the .NET SDK container tooling (no Dockerfile) and push it
dotnet publish EggLedger.API/EggLedger.API.csproj -c Release `
  -t:PublishContainer `
  -p:ContainerRegistry=eggledgeracr1.azurecr.io `
  -p:ContainerImageTag=v1

az acr repository show-tags --name eggledgeracr1 --repository eggledger-api -o table
```

## 5. Container Apps (API)

```powershell
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait

# Shared environment (owns logging + networking for the app)
az containerapp env create `
  --resource-group rg-eggledger-prod --name eggledger-env --location centralindia -o table

# Registry pull credentials
az acr update --name eggledgeracr1 --admin-enabled true
$acrPass = az acr credential show --name eggledgeracr1 --query "passwords[0].value" -o tsv

# Secret values (assembled in-shell; never committed)
$dbConn = "Host=eggledger-pg-prod1.postgres.database.azure.com;Port=5432;Database=eggledgerdb;Username=eggledgeradmin;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
$jwtProd = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 48 | %{[char]$_})   # new prod key
$googleId = "<google-client-id>"; $googleSecret = "<google-client-secret>"

# Create the app: image + secrets + env vars + external ingress on 8080, scale-to-zero.
# ASPNETCORE_ENVIRONMENT=Production makes the app load appsettings.Production.json and
# flip the refresh cookie to Secure=true; SameSite=None. Env vars use secretref: to point
# at the encrypted secrets rather than embedding values.
az containerapp create `
  --resource-group rg-eggledger-prod --name eggledger-api --environment eggledger-env `
  --image eggledgeracr1.azurecr.io/eggledger-api:v1 `
  --registry-server eggledgeracr1.azurecr.io `
  --registry-username eggledgeracr1 --registry-password $acrPass `
  --target-port 8080 --ingress external --min-replicas 0 --max-replicas 2 `
  --secrets "db-conn=$dbConn" "jwt-key=$jwtProd" "google-id=$googleId" "google-secret=$googleSecret" `
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "ConnectionStrings__DefaultConnection=secretref:db-conn" "Jwt__SecretKey=secretref:jwt-key" "Authentication__Google__ClientId=secretref:google-id" "Authentication__Google__ClientSecret=secretref:google-secret" `
  -o table

# API URL + health check
$apiUrl = "https://" + (az containerapp show -g rg-eggledger-prod -n eggledger-api --query "properties.configuration.ingress.fqdn" -o tsv)
curl.exe "$apiUrl/health"   # expect: Healthy
```

## 6. Static Web Apps (Vue)

Build + deploy run in GitHub Actions (`.github/workflows/azure-static-web-apps.yml`)
so no `swa` CLI is needed locally (managed devices can't install it from the npm feed).

```powershell
az staticwebapp create --name eggledger-web --resource-group rg-eggledger-prod --location eastasia -o table

# Deployment token -> GitHub repo SECRET  AZURE_STATIC_WEB_APPS_API_TOKEN
az staticwebapp secrets list -n eggledger-web -g rg-eggledger-prod --query "properties.apiKey" -o tsv

# Site URL
az staticwebapp show -n eggledger-web -g rg-eggledger-prod --query "defaultHostname" -o tsv
```

Then configure the repo (Settings > Secrets and variables > Actions):

- **Secret** `AZURE_STATIC_WEB_APPS_API_TOKEN` = the deployment token above.
- **Variable** `VITE_API_BASE_URL` = the **full API URL including `https://`**
  (e.g. `https://eggledger-api.<hash>.centralindia.azurecontainerapps.io`).

Run the "Deploy client to Azure Static Web Apps" workflow. `VITE_API_BASE_URL` is a
**Variable**, not a Secret, because the workflow reads it via `${{ vars.* }}`.

> **Two gotchas that cost real time here — both are missing schemes:**
> 1. `VITE_API_BASE_URL` must include `https://`. Without it, axios treats it as a
>    relative path and calls the site's own origin (405s on the static host).
> 2. It must be the **API** (Container Apps) URL, not the site's own Static Web Apps URL.
>
> A changed JS bundle hash after a deploy confirms a fresh build actually shipped.

## 7. Production CORS (and Google OAuth callback)

The API only sends `Access-Control-Allow-Origin` for an **exact** origin match, so the
value must include `https://` and no trailing slash — the same scheme gotcha as above.

```powershell
az containerapp update -g rg-eggledger-prod -n eggledger-api `
  --set-env-vars "Cors__AllowedOrigins__0=https://<your-swa-hostname>" -o table
```

Google OAuth requires two things behind the Container Apps ingress:

1. **Forwarded headers** must be enabled (see `Program.cs` / `MiddlewareExtensions`),
   or the app builds an `http://` redirect URI that Google rejects.
2. Register the correct redirect URI: it is the ASP.NET Core Google handler's
   **`CallbackPath` (default `/signin-google`)**, NOT the app's own
   `/egg-ledger-api/auth/google-callback` controller route. The handler processes
   `/signin-google` internally, then forwards to the controller (via the auth
   properties `RedirectUri`) to issue the JWT and set the cookie.

In the Google Cloud console, on the OAuth 2.0 Client ID:
- Authorized redirect URI: `https://<api-hostname>/signin-google` — **required**.
- Authorized JavaScript origins: **not used by this flow** (see section 13). Login is a
  server-side authorization-code flow, so Google only ever talks to the API. Values here
  are inert; they only matter if you add a browser-side Google SDK (One Tap / GIS).

The post-login redirect target is `allowedOrigins[0]` (the first `Cors__AllowedOrigins__*`
entry): the Google callback carries no `Origin` header, so `AuthController` falls back to
the first configured origin when building `{origin}/auth/callback`. Put the primary
frontend origin at index 0.

## 8. Production smoke test

- Open the Static Web Apps URL; `POST /auth/refresh` returns 401 on first load (healthy:
  reached the API, no session yet) rather than a 405/CORS error.
- Register a new account (email/password) and sign in with Google. Both should land on
  the dashboard signed in, with the refresh cookie set (HttpOnly + Secure + SameSite=None)
  and no tokens in localStorage.
- Hard reload keeps the session; logout clears the cookie.

## 9. Observability — Application Insights

The app already emits OpenTelemetry (traces/metrics/logs). `ServiceDefaults` enables the
Azure Monitor exporter when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, so it activates
in prod and stays inert in dev. App Insights is workspace-based, so it reuses the Log
Analytics workspace the Container Apps environment already created.

```powershell
az extension add -n application-insights 2>$null

# Reuse the workspace the ACA env created
$laId = az monitor log-analytics workspace show `
  -g rg-eggledger-prod -n <workspace-name> --query id -o tsv

az monitor app-insights component create `
  -g rg-eggledger-prod -a eggledger-ai -l centralindia `
  --workspace $laId --application-type web -o table

$aiConn = az monitor app-insights component show `
  -g rg-eggledger-prod -a eggledger-ai --query connectionString -o tsv

# Store as a secret, reference as the env var the code reads
az containerapp secret set -g rg-eggledger-prod -n eggledger-api --secrets appinsights-conn="$aiConn"
az containerapp update -g rg-eggledger-prod -n eggledger-api `
  --set-env-vars "APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-conn" -o table
```

> Telemetry only flows once an image containing the exporter code is deployed (the env var
> alone does nothing on an older image). Redeploy the API after enabling it.

The client stamps its build (`VITE_APP_VERSION` = commit SHA) and sends it as
`X-Client-Version`; the API tags each request span with `client.version`. Correlate in KQL:

```kusto
requests | summarize count() by tostring(customDimensions['client.version'])
```

## 10. Managed identity — ACR pull (drop admin credentials)

Replace the registry admin username/password with the Container App's system-assigned
identity + the `AcrPull` role.

```powershell
az containerapp identity assign -g rg-eggledger-prod -n eggledger-api --system-assigned -o table
$miPrincipal = az containerapp identity show -g rg-eggledger-prod -n eggledger-api --query principalId -o tsv
$acrId       = az acr show -n eggledgeracr1 --query id -o tsv
az role assignment create --assignee $miPrincipal --role AcrPull --scope $acrId -o table
az containerapp registry set -g rg-eggledger-prod -n eggledger-api `
  --server eggledgeracr1.azurecr.io --identity system -o table
```

> Verify before removing the fallback: restart the active revision (forces a fresh pull via
> the identity) and confirm `/health` is 200. Only then disable admin:
> `az acr update -n eggledgeracr1 --admin-enabled false`. Also remove the now-orphaned
> `eggledgeracr1azurecrio-eggledgeracr1` secret. Role propagation can take ~1 min.

## 11. Key Vault — secret references

Move the inline ACA secrets into Key Vault; the app config then holds only references, and
the same managed identity reads them. RBAC has two planes: Owner can manage the vault but
cannot read/write secret values — that needs a data-plane role.

```powershell
az keyvault create -g rg-eggledger-prod -n eggledger-kv-prod1 -l centralindia `
  --enable-rbac-authorization true -o table

$kvId        = az keyvault show -n eggledger-kv-prod1 --query id -o tsv
$me          = az ad signed-in-user show --query id -o tsv
$miPrincipal = az containerapp identity show -g rg-eggledger-prod -n eggledger-api --query principalId -o tsv
az role assignment create --assignee $me --role "Key Vault Secrets Officer" --scope $kvId -o table
az role assignment create --assignee $miPrincipal --role "Key Vault Secrets User" --scope $kvId -o table

# Copy each existing ACA secret value into Key Vault (values never printed)
foreach ($s in 'google-id','google-secret','jwt-key','db-conn','appinsights-conn') {
  $v = az containerapp secret show -g rg-eggledger-prod -n eggledger-api --secret-name $s --query value -o tsv
  az keyvault secret set --vault-name eggledger-kv-prod1 --name $s --value "$v" -o none
}

# Flip each ACA secret to a Key Vault reference resolved by the identity
$kvUri = "https://eggledger-kv-prod1.vault.azure.net/secrets"
az containerapp secret set -g rg-eggledger-prod -n eggledger-api --secrets `
  "google-id=keyvaultref:$kvUri/google-id,identityref:system" `
  "google-secret=keyvaultref:$kvUri/google-secret,identityref:system" `
  "jwt-key=keyvaultref:$kvUri/jwt-key,identityref:system" `
  "db-conn=keyvaultref:$kvUri/db-conn,identityref:system" `
  "appinsights-conn=keyvaultref:$kvUri/appinsights-conn,identityref:system" -o table
```

> Restart the revision and confirm `/health` 200 + a working login (exercises JWT/Google/DB
> secrets) to prove the identity reads Key Vault before trusting the migration.

## 12. Custom domain + TLS (Static Web Apps)

TLS certificates are free and auto-managed; the only cost is domain registration. A
subdomain needs one CNAME. With Cloudflare DNS, set the record to **DNS only (grey cloud)** —
proxying (orange cloud) resolves the CNAME to Cloudflare and blocks Azure's validation +
managed cert.

```
# Cloudflare DNS record
Type=CNAME  Name=eggledger  Target=<swa-default-hostname>  Proxy=DNS only
```

```powershell
az staticwebapp hostname set -g rg-eggledger-prod -n eggledger-web `
  --hostname eggledger.sshnk.com -o table
az staticwebapp hostname list -g rg-eggledger-prod -n eggledger-web -o table   # wait for Ready
```

Changing the **frontend** origin only requires updating the API's CORS list (which also sets
the OAuth redirect target — see section 7). Make the custom domain index 0:

```powershell
az containerapp update -g rg-eggledger-prod -n eggledger-api --set-env-vars `
  "Cors__AllowedOrigins__0=https://eggledger.sshnk.com" `
  "Cors__AllowedOrigins__1=https://<swa-default-hostname>" -o table
```

No frontend rebuild, no CSP change, and no Google console change are needed — the API domain
(and thus the OAuth redirect URI) is unchanged. A custom domain on the **API** would be
different: it ripples into `VITE_API_BASE_URL`, CORS, the CSP `connect-src`, and Google's
Authorized redirect URIs — see the next section.

## 12b. Custom domain + TLS (API / Container Apps)

Container Apps needs **two** DNS records (unlike SWA's single CNAME): a **CNAME** for routing
and a **TXT `asuid.<sub>`** record to prove ownership. TLS stays free and managed. Both records
are **DNS only (grey cloud)** in Cloudflare.

```powershell
# 1. Ownership token for the TXT record + the CNAME target (current app FQDN)
$verifyId = az containerapp show -g rg-eggledger-prod -n eggledger-api `
  --query "properties.customDomainVerificationId" -o tsv
az containerapp show -g rg-eggledger-prod -n eggledger-api `
  --query "properties.configuration.ingress.fqdn" -o tsv
```

```
# Cloudflare DNS records (both DNS only)
Type=CNAME  Name=api        Target=<app-fqdn>.azurecontainerapps.io
Type=TXT    Name=asuid.api  Value=<verifyId>
```

```powershell
# 2. After DNS resolves, add the hostname and bind a managed cert
az containerapp hostname add -g rg-eggledger-prod -n eggledger-api --hostname api.sshnk.com -o table
az containerapp hostname bind -g rg-eggledger-prod -n eggledger-api `
  --hostname api.sshnk.com --environment eggledger-env --validation-method CNAME -o table
curl.exe -i https://api.sshnk.com/health   # expect 200 Healthy with a valid cert
```

Moving the **API** to a custom domain is the four-place ripple:

1. **`VITE_API_BASE_URL`** GitHub Actions Variable → `https://api.sshnk.com` (triggers a client rebuild).
2. **CSP `connect-src`** in `staticwebapp.config.json` → add `https://api.sshnk.com` (keep the old
   ACA host during the cutover, then drop it).
3. **Google Authorized redirect URI** → add `https://api.sshnk.com/signin-google` (keep the old
   one during the cutover).
4. **CORS does not change** — it lists the *frontend* origin (`eggledger.sshnk.com`), which is
   unchanged.

> Bonus: with `eggledger.sshnk.com` and `api.sshnk.com` both under `sshnk.com`, frontend and API
> are now **same-site**, so the refresh cookie could be tightened from `SameSite=None` to `Lax`.

## 13. Google OAuth topology (why the custom frontend domain "just worked")

This app uses the **authorization-code flow with a confidential client**: the API — not the
browser — is the OAuth client. It holds the client secret and does the code↔token exchange
server-to-server; the browser only ever receives an HttpOnly cookie (no tokens in the URL).

Flow:

1. Browser navigates to `<API>/egg-ledger-api/auth/google-login` (a full-page redirect).
2. API 302s to Google with `redirect_uri=<API>/signin-google`.
3. Google validates that against **Authorized redirect URIs**, shows consent.
4. Google redirects to `<API>/signin-google?code=...`.
5. API exchanges the code, signs in, sets the refresh cookie.
6. API redirects to `{allowedOrigins[0]}/auth/callback` — the only step that touches the
   frontend domain, and it is driven by the API's own CORS config, not by Google.

Consequences:

- **Authorized redirect URIs** point at the **API** and are what make login work. Changing
  the frontend domain needs no Google change; changing the **API** domain does (add
  `https://<new-api>/signin-google`).
- **Authorized JavaScript origins** are an origin allowlist for browser-side Google SDK calls
  (One Tap / GIS `initTokenClient`). This flow never calls Google from JS, so the field is
  unused here — login from `eggledger.sshnk.com` succeeds even though it is not listed.

### Consent-screen branding

The "to continue to …" text and the small domain shown on Google's account chooser come from
the **OAuth consent screen** (APIs & Services → Branding), not from the redirect URI. Set:

- **App name** = `EggLedger` (this replaces a raw host in the "continue to" text)
- **User support email**, and an **App logo** (optional)
- **Application home page** = `https://eggledger.sshnk.com`
- **Authorized domain** = `sshnk.com`

To fully brand the host shown mid-flow (so no `azurecontainerapps.io` appears at all), give
the **API** a custom domain (e.g. `api.sshnk.com`) and re-register its `/signin-google`
redirect URI — see the API-domain ripple note in section 12. Removing the "Google hasn't
verified this app" warning for external users requires OAuth verification (domain ownership
in Search Console + review); for a small user base you can stay in Testing with test users.

