# Secrets & Configuration

EggLedger keeps **non-secret** configuration in git (`appsettings.json`,
`appsettings.Production.json`) and keeps **secrets out of the repo entirely**.

## What counts as a secret

| Config key | Why it's secret |
| --- | --- |
| `Jwt:SecretKey` | Signs every JWT. Anyone with it can forge tokens. |
| `ConnectionStrings:DefaultConnection` | Contains the database password. |
| `Authentication:Google:ClientSecret` | Google OAuth client secret. |
| `Authentication:Google:ClientId` | Semi-public, but treated as a secret here. |

Everything else (JWT issuer/audience/expiry, CORS origins, log levels, `Ef_Migrate`)
is non-secret and lives in the committed `appsettings*.json` files.

## How configuration layers (later wins)

```
appsettings.json  →  appsettings.{Environment}.json  →  User Secrets (dev)  →  Environment variables (prod)
```

.NET merges these at startup, so a secret provided by User Secrets or an
environment variable transparently fills in the keys omitted from the committed files.

## Development — .NET User Secrets

Secrets are stored per-machine outside the repo (on Windows:
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`). No files to zip or share.

Set them once:

```powershell
dotnet user-secrets set "Jwt:SecretKey" "<32+ char random key>" --project EggLedger.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Username=<user>;Password=<pwd>;Database=eggledgerDB;" --project EggLedger.API
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project EggLedger.API
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project EggLedger.API
```

Inspect (keys + values) or clear:

```powershell
dotnet user-secrets list --project EggLedger.API
dotnet user-secrets clear --project EggLedger.API
```

Generate a strong JWT signing key:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
```

## Production — environment variables

.NET maps nested config keys to environment variables by replacing `:` with a
double underscore `__`. In Azure these are injected as **Container Apps secrets**
(Phase 3), never committed:

| Config key | Environment variable |
| --- | --- |
| `Jwt:SecretKey` | `Jwt__SecretKey` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |

Use a **new** JWT key and DB password in production — never reuse dev values.

## Rules

- Never put a real secret in any committed `appsettings*.json`.
- `appsettings.Development.json` is gitignored; use it only for local, non-shared overrides.
- Startup validation (`ValidateOnStart`) fails fast if a required secret is missing.
