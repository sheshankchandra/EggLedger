# Production Deployment Guide (Azure)

This guide provides end-to-end instructions for deploying EggLedger to Microsoft Azure using the Azure CLI (`az`).

---

## Architecture Overview

EggLedger is architected for low cost, high availability, and strong security:

```text
  Internet
     │
     ├──► eggledger.sshnk.com ──► Azure Static Web Apps (Vue 3 SPA)
     │                                    │
     │                                (Axios API)
     │                                    ▼
     └──► api.sshnk.com       ──► Azure Container Apps (ASP.NET Core API)
                                          │
                  ┌───────────────────────┼───────────────────────┐
                  ▼                       ▼                       ▼
          PostgreSQL Server        Azure Key Vault        Application Insights
       (Flexible Server B1ms)     (Managed Identity)        (OpenTelemetry)
```

| Component | Azure Service | Configuration / SKU |
| --- | --- | --- |
| **Frontend SPA** | Azure Static Web Apps | Free Tier, custom domain with managed TLS |
| **Backend API** | Azure Container Apps | Serverless consumption (scale-to-zero), port 8080 |
| **Database** | Azure Database for PostgreSQL | Flexible Server (Burstable B1ms), PG 16 |
| **Container Registry**| Azure Container Registry (ACR) | Basic SKU, pulled via Managed Identity (`AcrPull`) |
| **Secrets Management**| Azure Key Vault | RBAC data-plane references |
| **Observability** | Azure Application Insights | OpenTelemetry telemetry via Log Analytics |

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (`az`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for container builds and tests)
- An active Azure subscription

Register the required resource providers:

```bash
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
```

---

## Deployment Configuration Variables

Set these shell variables before running the commands below:

```bash
RESOURCE_GROUP="rg-eggledger-prod"
LOCATION="centralindia"
PG_SERVER="eggledger-pg"
DB_NAME="eggledgerdb"
DB_ADMIN="eggledgeradmin"
ACR_NAME="eggledgeracr"
ACA_ENV="eggledger-env"
API_APP_NAME="eggledger-api"
SWA_APP_NAME="eggledger-web"
KEY_VAULT_NAME="eggledger-kv"
APP_INSIGHTS_NAME="eggledger-ai"
CUSTOM_WEB_DOMAIN="eggledger.sshnk.com"
CUSTOM_API_DOMAIN="api.sshnk.com"
```

---

## 1. Resource Group

Create a unified resource group for all project services:

```bash
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" -o table
```

---

## 2. PostgreSQL Flexible Server

Provision a PostgreSQL Flexible Server and create the application database:

```bash
# Provision server (replace <DB_PASSWORD> with a strong alphanumeric password)
az postgres flexible-server create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$PG_SERVER" \
  --location "$LOCATION" \
  --tier Burstable --sku-name Standard_B1ms \
  --storage-size 32 --version 16 \
  --admin-user "$DB_ADMIN" --admin-password "<DB_PASSWORD>" \
  --public-access None -o table

# Allow traffic from Azure Container Apps
az postgres flexible-server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" --server-name "$PG_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o table

# Temporarily allow deployer IP for schema migration
MY_IP=$(curl -s https://api.ipify.org)
az postgres flexible-server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" --server-name "$PG_SERVER" \
  --name AllowDeployerIP --start-ip-address "$MY_IP" --end-ip-address "$MY_IP" -o table

# Create the application database
az postgres flexible-server db create \
  --resource-group "$RESOURCE_GROUP" --server-name "$PG_SERVER" \
  --name "$DB_NAME" -o table
```

---

## 3. Database Migrations

Apply pending EF Core migrations to the production database:

```bash
DB_CONN="Host=${PG_SERVER}.postgres.database.azure.com;Port=5432;Database=${DB_NAME};Username=${DB_ADMIN};Password=<DB_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"

dotnet ef database update \
  --project EggLedger.Data \
  --startup-project EggLedger.API \
  --connection "$DB_CONN"
```

> [!TIP]
> For zero-downtime continuous deployment, generate an idempotent SQL script instead and execute it via `psql`. See [`docs/MIGRATIONS.md`](MIGRATIONS.md).

---

## 4. Container Registry (ACR) & Image Publish

The API uses the built-in .NET SDK container tooling (`PublishContainer`), avoiding external Dockerfile maintenance.

```bash
# Create Azure Container Registry
az acr create --resource-group "$RESOURCE_GROUP" --name "$ACR_NAME" --sku Basic -o table
az acr login --name "$ACR_NAME"

# Build and push API container image
dotnet publish EggLedger.API/EggLedger.API.csproj -c Release \
  -t:PublishContainer \
  -p:ContainerRegistry="${ACR_NAME}.azurecr.io" \
  -p:ContainerImageTag="v1"
```

---

## 5. Observability (Application Insights)

Create an Application Insights resource connected to Log Analytics:

```bash
# Create shared Container Apps environment
az containerapp env create \
  --resource-group "$RESOURCE_GROUP" --name "$ACA_ENV" --location "$LOCATION" -o table

# Get default Log Analytics Workspace ID created by the environment
WORKSPACE_ID=$(az monitor log-analytics workspace list \
  --resource-group "$RESOURCE_GROUP" --query "[0].id" -o tsv)

# Create Application Insights component
az monitor app-insights component create \
  --resource-group "$RESOURCE_GROUP" --app "$APP_INSIGHTS_NAME" --location "$LOCATION" \
  --workspace "$WORKSPACE_ID" --application-type web -o table

# Retrieve connection string
AI_CONN=$(az monitor app-insights component show \
  --resource-group "$RESOURCE_GROUP" --app "$APP_INSIGHTS_NAME" \
  --query connectionString -o tsv)
```

---

## 6. Secrets & Key Vault Integration

Store application secrets in Azure Key Vault and access them securely using the Container App's Managed Identity.

```bash
# Create Key Vault with Azure RBAC enabled
az keyvault create \
  --resource-group "$RESOURCE_GROUP" --name "$KEY_VAULT_NAME" --location "$LOCATION" \
  --enable-rbac-authorization true -o table

KV_ID=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)
CURRENT_USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Grant deployment user permission to write secrets
az role assignment create \
  --assignee "$CURRENT_USER_ID" --role "Key Vault Secrets Officer" --scope "$KV_ID" -o table

# Set secrets
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "db-conn" --value "$DB_CONN" -o none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "jwt-key" --value "<SECURE_32_CHAR_JWT_KEY>" -o none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "google-id" --value "<GOOGLE_CLIENT_ID>" -o none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "google-secret" --value "<GOOGLE_CLIENT_SECRET>" -o none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "appinsights-conn" --value "$AI_CONN" -o none
```

---

## 7. Azure Container Apps (API)

Create the Container App with System-Assigned Managed Identity, linking ACR and Key Vault:

```bash
# 1. Create Container App
az containerapp create \
  --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --environment "$ACA_ENV" \
  --image "${ACR_NAME}.azurecr.io/eggledger-api:v1" \
  --target-port 8080 --ingress external --min-replicas 0 --max-replicas 2 \
  -o table

# 2. Enable System-Assigned Managed Identity
az containerapp identity assign \
  --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --system-assigned -o table

PRINCIPAL_ID=$(az containerapp identity show \
  --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --query principalId -o tsv)

# 3. Grant AcrPull to Managed Identity
ACR_ID=$(az acr show --name "$ACR_NAME" --query id -o tsv)
az role assignment create --assignee "$PRINCIPAL_ID" --role AcrPull --scope "$ACR_ID" -o table

az containerapp registry set \
  --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" \
  --server "${ACR_NAME}.azurecr.io" --identity system -o table

# 4. Grant Key Vault Secrets User to Managed Identity
az role assignment create --assignee "$PRINCIPAL_ID" --role "Key Vault Secrets User" --scope "$KV_ID" -o table

# 5. Configure Key Vault secret references in Container Apps
KV_URI="https://${KEY_VAULT_NAME}.vault.azure.net/secrets"
az containerapp secret set --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --secrets \
  "db-conn=keyvaultref:${KV_URI}/db-conn,identityref:system" \
  "jwt-key=keyvaultref:${KV_URI}/jwt-key,identityref:system" \
  "google-id=keyvaultref:${KV_URI}/google-id,identityref:system" \
  "google-secret=keyvaultref:${KV_URI}/google-secret,identityref:system" \
  "appinsights-conn=keyvaultref:${KV_URI}/appinsights-conn,identityref:system" -o table

# 6. Map secret references to application environment variables
az containerapp update --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --set-env-vars \
  "ASPNETCORE_ENVIRONMENT=Production" \
  "ConnectionStrings__DefaultConnection=secretref:db-conn" \
  "Jwt__SecretKey=secretref:jwt-key" \
  "Authentication__Google__ClientId=secretref:google-id" \
  "Authentication__Google__ClientSecret=secretref:google-secret" \
  "APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-conn" \
  "Cors__AllowedOrigins__0=https://${CUSTOM_WEB_DOMAIN}" -o table
```

Verify deployment health:

```bash
API_FQDN=$(az containerapp show --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --query "properties.configuration.ingress.fqdn" -o tsv)
curl -i "https://${API_FQDN}/health"
```

---

## 8. Azure Static Web Apps (Frontend)

Deploy the Vue 3 Single Page Application to Azure Static Web Apps:

```bash
az staticwebapp create \
  --resource-group "$RESOURCE_GROUP" --name "$SWA_APP_NAME" --location "eastasia" -o table

# Retrieve deployment API token for GitHub Actions
SWA_TOKEN=$(az staticwebapp secrets list \
  --resource-group "$RESOURCE_GROUP" --name "$SWA_APP_NAME" --query "properties.apiKey" -o tsv)
```

Configure GitHub repository settings (**Settings > Secrets and variables > Actions**):
- **Secret**: `AZURE_STATIC_WEB_APPS_API_TOKEN` = `$SWA_TOKEN`
- **Variable**: `VITE_API_BASE_URL` = `https://${CUSTOM_API_DOMAIN}` (must include `https://`)

The build and deployment process is fully automated via `.github/workflows/azure-static-web-apps.yml`.

---

## 9. Custom Domains & TLS Configuration

Both Azure Static Web Apps and Azure Container Apps provide free, auto-renewing managed TLS certificates.

### Frontend Custom Domain (Static Web Apps)

1. Add a **CNAME** record in your DNS provider (e.g., Cloudflare with **DNS-only / Grey Cloud**):
   ```text
   Type: CNAME | Name: eggledger | Target: <your-swa-default-hostname>.azurestaticapps.net
   ```
2. Bind the custom hostname:
   ```bash
   az staticwebapp hostname set \
     --resource-group "$RESOURCE_GROUP" --name "$SWA_APP_NAME" \
     --hostname "$CUSTOM_WEB_DOMAIN" -o table
   ```

### Backend Custom Domain (Container Apps)

1. Obtain the verification ID:
   ```bash
   VERIFY_ID=$(az containerapp show \
     --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" \
     --query "properties.customDomainVerificationId" -o tsv)
   ```
2. Add the required DNS records (**DNS-only / Grey Cloud**):
   ```text
   Type: CNAME | Name: api        | Target: <your-api-fqdn>.azurecontainerapps.io
   Type: TXT   | Name: asuid.api  | Value:  <VERIFY_ID>
   ```
3. Bind the hostname and issue a managed certificate:
   ```bash
   az containerapp hostname add \
     --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" \
     --hostname "$CUSTOM_API_DOMAIN" -o table

   az containerapp hostname bind \
     --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" \
     --hostname "$CUSTOM_API_DOMAIN" --environment "$ACA_ENV" --validation-method CNAME -o table
   ```

---

## 10. Google OAuth 2.0 Topology

EggLedger implements the **confidential authorization-code flow**:

1. User clicks **Login with Google** in the SPA.
2. The browser initiates `GET /egg-ledger-api/auth/google-login` on the API.
3. The API issues a redirect to Google Accounts with `redirect_uri=https://<API_DOMAIN>/signin-google`.
4. After consent, Google redirects the browser back to `https://<API_DOMAIN>/signin-google` with an authorization code.
5. The API exchanges the code with Google server-to-server, establishes a user session, sets the `Secure; HttpOnly; SameSite=None` refresh cookie, and redirects the browser back to `{Cors__AllowedOrigins__0}/auth/callback`.

### Google Cloud Console Configuration

In the [Google Cloud Console](https://console.cloud.google.com/) under **APIs & Services > Credentials**:
- **Authorized Redirect URIs**: `https://<API_DOMAIN>/signin-google` (e.g., `https://api.sshnk.com/signin-google`).
- **Authorized JavaScript Origins**: Not required for this confidential server-side code flow.

> [!IMPORTANT]
> The API relies on `ForwardedHeadersMiddleware` (`X-Forwarded-Proto` and `X-Forwarded-Host`) to ensure the ASP.NET Core Google authentication handler generates HTTPS callback URLs when running behind the Container Apps ingress.

---

## 11. Redeploying the API

To publish updates to the backend API:

```bash
# 1. Build and push new container tag
dotnet publish EggLedger.API/EggLedger.API.csproj -c Release \
  -t:PublishContainer \
  -p:ContainerRegistry="${ACR_NAME}.azurecr.io" \
  -p:ContainerImageTag="v2"

# 2. Update Container App to point to the new tag
az containerapp update \
  --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" \
  --image "${ACR_NAME}.azurecr.io/eggledger-api:v2" -o table

# 3. Verify health probe
curl -i "https://${CUSTOM_API_DOMAIN}/health"
```
