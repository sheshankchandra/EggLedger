# EggLedger

A full-stack app for shared households: create a room, track shared groceries and supplies as
"containers", and record stock/consumption "orders" that keep everyone's balance up to date.

Built as a hands-on learning project, then taken all the way to a hardened production deployment
on Azure.

**Live:** [eggledger.sshnk.com](https://eggledger.sshnk.com) · API at
[api.sshnk.com](https://api.sshnk.com)

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Vue.js](https://img.shields.io/badge/Vue.js-3.x-green.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)

## Features

- **Rooms** with unique join codes, and admin/member roles
- **Containers** for shared items, with stock and consumption tracking
- **Orders** (stock / consume) that update balances automatically
- **Auth** with JWT access tokens and Google OAuth 2.0 (server-side code flow)
- **Secure session model**: in-memory access token + HttpOnly refresh cookie, silent refresh,
  anti-CSRF header
- **OpenAPI** docs via Scalar in development

## Tech stack

| Area | Choice |
| --- | --- |
| Backend | ASP.NET Core (.NET 10), EF Core, FluentResults |
| Frontend | Vue 3 (Composition API), Vite, Pinia, Vue Router |
| Database | PostgreSQL 15 |
| Dev orchestration | .NET Aspire 13 (Postgres + API + Vite + pgWeb) |
| Auth | JWT, Google OAuth 2.0 |
| Tests | xUnit + Testcontainers (`WebApplicationFactory`) |
| Observability | OpenTelemetry → Application Insights (prod) / Aspire dashboard (dev) |
| Hosting | Azure Container Apps (API) + Static Web Apps (SPA) + Postgres Flexible Server |

## Architecture

The backend is layered; each layer has a clear responsibility:

```
EggLedger/
├── EggLedger.API/              # Controllers (thin), middleware, DI/config, Program.cs
├── EggLedger.Services/         # Business logic (returns FluentResults)
├── EggLedger.Data/             # DbContext, EF Core config, migrations
├── EggLedger.Models/           # Domain entities (DB-mapped)
├── EggLedger.DTO/              # Request/response shapes (never return entities)
├── EggLedger.ServiceDefaults/  # Aspire defaults: health checks, OpenTelemetry
├── EggLedger.AppHost/          # .NET Aspire orchestration (development only)
├── EggLedger.Client/           # Vue 3 SPA
└── EggLedger.Tests/            # xUnit integration tests (Testcontainers)
```

API routes are prefixed with `/egg-ledger-api/`. Controllers validate input and delegate; all
business logic lives in the Services layer.

## Quick start (recommended: .NET Aspire)

.NET Aspire orchestrates the whole stack — it starts PostgreSQL in a container, runs the API,
launches the Vite dev server, and adds pgWeb for database browsing, all behind one dashboard.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20.19+ or 22.12+](https://nodejs.org/) (required by Vite 8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the Postgres container)

### Run

```bash
git clone https://github.com/sheshankchandra/EggLedger.git
cd EggLedger

# Frontend deps (Aspire also runs `npm ci` on start, but this warms the cache)
cd EggLedger.Client && npm install && cd ..

# Start everything
dotnet run --project EggLedger.AppHost
```

Then open the **Aspire dashboard** at `https://localhost:17071`. From there you can open the Vue
client (Aspire assigns it a port), inspect logs/traces/metrics, and open **pgWeb** to browse the
database. The AppHost injects `VITE_API_BASE_URL` so the SPA and the OAuth start URL target the
API automatically.

> Running the client on its own with `npm run dev` works only if you also provide
> `VITE_API_BASE_URL` yourself — otherwise Google login has no API URL to redirect to. The Aspire
> flow is the intended dev loop.

## Configuration

Local secrets are read from **.NET User Secrets** in development (never committed).
`EggLedger.API/appsettings-example.json` shows the shape of the non-secret settings. The keys the
app expects:

```jsonc
{
  "Jwt": {
    "SecretKey": "<long random key>",
    "Issuer": "EggLedgerAPI",
    "Audience": "EggLedgerAudience",
    "ExpiryInMinutes": 15
  },
  "ConnectionStrings": { "DefaultConnection": "Host=...;Database=eggledgerDB;..." },
  "Authentication": { "Google": { "ClientId": "...", "ClientSecret": "..." } },
  "Cors": { "AllowedOrigins": ["http://localhost:5173"], "PolicyName": "_myAllowSpecificOrigins" },
  "Ef_Migrate": "true"   // dev only; false in production (migrations run deliberately)
}
```

See [`docs/SECRETS.md`](docs/SECRETS.md) for the User Secrets workflow and
[`docs/MIGRATIONS.md`](docs/MIGRATIONS.md) for how migrations are gated.

## API overview

Base path: `/egg-ledger-api`. A few representative endpoints:

- **Auth** — `POST /auth/login`, `POST /auth/register`, `POST /auth/refresh`,
  `POST /auth/logout`, `GET /auth/google-login`
- **Rooms** — `POST /room/create`, `POST /room/join`, `GET /room/user/all`
- **Containers** — `GET /room/{roomCode}/container/all`, `POST /room/{roomCode}/container/create`
- **Orders** — `POST /{roomCode}/orders/stock`, `POST /{roomCode}/orders/consume`

Full interactive docs (Scalar) at `http://localhost:8080/scalar/v2` when the API runs in
development.

## Security model

- Access tokens are held **in memory** on the client; the **refresh token is an HttpOnly cookie**
  (`Secure` + `SameSite=None` in prod). Tokens never touch `localStorage` or URLs.
- Cookie-authenticated endpoints require a custom **anti-CSRF header** (`X-EggLedger-CSRF`), which
  forces a CORS preflight a cross-site page can't forge.
- **CORS** is restricted to configured origins (no `AllowAnyOrigin`).
- **Rate limiting**: a global per-IP budget plus a stricter budget on auth endpoints (429 on
  breach), both tunable via `RateLimiting:*` config.
- **HSTS** in production; forwarded headers honored behind the Container Apps ingress.
- Google login uses the **authorization-code flow with a confidential client** — the API holds the
  secret and does the token exchange; the browser only ever gets the cookie.

## Testing

```bash
dotnet test
```

`EggLedger.Tests` boots the real API in-process via `WebApplicationFactory<Program>` against a
throwaway PostgreSQL container (Testcontainers), so **Docker must be running**. It covers the auth
flow: register, login, refresh rotation, CSRF, and rate limiting. CI runs `dotnet test` on every
PR.

## Deployment

The app runs on Azure: the SPA on **Static Web Apps**, the API on **Container Apps**
(scale-to-zero) pulling its image from **Container Registry** via **managed identity**, backed by
**PostgreSQL Flexible Server**, with secrets in **Key Vault** and telemetry in **Application
Insights**. Custom domains (`eggledger.sshnk.com`, `api.sshnk.com`) use free managed TLS.

The full runbook — every `az` command, DNS record, and the gotchas learned along the way — is in
[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

## Building for production

```bash
# API container image (SDK container tooling, no Dockerfile)
dotnet publish EggLedger.API/EggLedger.API.csproj -c Release -t:PublishContainer

# Frontend
cd EggLedger.Client && npm run build
```

## Contributing

1. Create a feature branch
2. Make your changes, matching the existing layering and conventions
3. Add or adjust tests for behavior you change
4. Open a pull request (CI runs build, tests, and lint)

## License

MIT — see [LICENSE](LICENSE).
