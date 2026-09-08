# Secrets & Configuration Management

EggLedger separates configuration from code: **non-sensitive configuration** is checked into version control (`appsettings.json`, `appsettings.Production.json`), while **secrets and credentials** are strictly managed outside the repository.

---

## Secret Configuration Keys

The following keys contain credentials and must **never** be committed to version control:

| Configuration Key | Purpose |
| --- | --- |
| `Jwt:SecretKey` | Cryptographic key used to sign and verify JWT access tokens (minimum 256-bit). |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string containing database credentials. |
| `Authentication:Google:ClientId` | Google OAuth 2.0 Web Client ID. |
| `Authentication:Google:ClientSecret` | Google OAuth 2.0 Client Secret for server-side token exchange. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Application Insights connection string for OpenTelemetry telemetry. |

All non-sensitive settings (JWT issuer, audience, token lifetimes, CORS origins, log levels, and rate limiting thresholds) reside in the committed `appsettings*.json` files.

---

## Configuration Layering

ASP.NET Core resolves configuration in the following order (later sources override earlier ones):

```text
appsettings.json 
  └── appsettings.{Environment}.json 
        └── .NET User Secrets (Development) 
              └── Environment Variables / Cloud Secret Store (Production)
```

Missing values are transparently populated from the appropriate environment-specific provider without modifying committed configuration templates.

---

## Local Development (.NET User Secrets)

For local development, use [.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store credentials outside the project tree.

### Setting Secrets

```bash
# Set JWT signing key
dotnet user-secrets set "Jwt:SecretKey" "<your-secret-key-at-least-32-chars>" --project EggLedger.API

# Set local PostgreSQL connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Username=eggledger;Password=eggledger123;Database=eggledgerDB;" --project EggLedger.API

# Set Google OAuth credentials (optional for local email/password auth)
dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>" --project EggLedger.API
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>" --project EggLedger.API
```

### Inspecting Secrets

```bash
# List all configured secrets
dotnet user-secrets list --project EggLedger.API

# Clear stored secrets
dotnet user-secrets clear --project EggLedger.API
```

### Generating a Secure JWT Signing Key

To generate a cryptographically strong 256-bit base64-encoded key:

- **PowerShell:**
  ```powershell
  [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
  ```

- **Bash / OpenSSL:**
  ```bash
  openssl rand -base64 48
  ```

---

## Production (Environment Variables & Key Vault)

In production environments (e.g., Azure Container Apps or Docker containers), nested JSON configuration keys map to environment variables using a double underscore (`__`):

| Configuration Key | Environment Variable |
| --- | --- |
| `Jwt:SecretKey` | `Jwt__SecretKey` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `APPLICATIONINSIGHTS_CONNECTION_STRING` |

### Cloud Secret References

In production deployments on Azure Container Apps, environment variables should reference secrets stored in **Azure Key Vault** using Managed Identity references (e.g., `keyvaultref:https://<vault-name>.vault.azure.net/secrets/<secret-name>,identityref:system`), avoiding raw values in deployment manifests.

See the [Deployment Guide](DEPLOYMENT.md) for full setup instructions.

---

## Best Practices

- **Never Commit Secrets**: Ensure `appsettings.Development.json` and `.env` files remain in `.gitignore`.
- **Environment Isolation**: Always generate unique JWT keys and database passwords for production environments.
- **Fail-Fast Validation**: The API employs startup validation (`ValidateOnStart`) to verify that all required secret keys are present before accepting incoming traffic.
