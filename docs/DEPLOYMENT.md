# Azure Deployment Runbook

Manual deployment of EggLedger to Azure using the `az` CLI. Target architecture:

| Component | Azure service |
| --- | --- |
| Vue SPA | Static Web Apps |
| .NET API | Container Apps (scale-to-zero) |
| Database | PostgreSQL Flexible Server (Burstable B1ms) |
| API image | Container Registry |

Secrets (JWT key, DB connection, Google OAuth) are injected as Container Apps
secrets / environment variables — never committed. See `docs/SECRETS.md`.

> Status: work in progress. Steps 1–3 are done; 4+ are being filled in as we go.

## Prerequisites

```powershell
winget install Microsoft.AzureCLI            # Azure CLI
az login --tenant <tenant-id>                # sign in to the tenant that holds the subscription
az account show -o table                     # confirm the active subscription
```

Docker Desktop and the .NET 9 runtime are also required locally.

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
- Authorized JavaScript origin: `https://<swa-hostname>`
- Authorized redirect URI: `https://<api-hostname>/signin-google`

## 8. Production smoke test

- Open the Static Web Apps URL; `POST /auth/refresh` returns 401 on first load (healthy:
  reached the API, no session yet) rather than a 405/CORS error.
- Register a new account (email/password) and sign in with Google. Both should land on
  the dashboard signed in, with the refresh cookie set (HttpOnly + Secure + SameSite=None)
  and no tokens in localStorage.
- Hard reload keeps the session; logout clears the cookie.

