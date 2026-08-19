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

# App deploy (image + secrets + env vars + ingress) is filled in as we complete it.
# Secrets: DB connection string, production JWT key, Google client id/secret.
# Env vars: ASPNETCORE_ENVIRONMENT=Production, Ef_Migrate=false, secretref pointers,
#           and Cors__AllowedOrigins__0 set to the Static Web Apps URL (step 7).
```

## 6. Static Web Apps (Vue) — _todo_

## 7. Production CORS + cookie + Google OAuth callback — _todo_

## 8. Production smoke test — _todo_
