# EggLedger

[![CI](https://github.com/sheshankchandra/EggLedger/actions/workflows/ci.yml/badge.svg)](https://github.com/sheshankchandra/EggLedger/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Vue.js](https://img.shields.io/badge/Vue.js-3.x-4FC08D?logo=vuedotjs&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-4169E1?logo=postgresql&logoColor=white)
![Aspire](https://img.shields.io/badge/.NET_Aspire-13-512BD4?logo=dotnet&logoColor=white)

EggLedger is a modern full-stack web application designed for shared households and roommates to effortlessly track communal groceries, manage supplies, split expenses, and settle debts with complete clarity.

**Live Application:** [eggledger.sshnk.com](https://eggledger.sshnk.com) · **API:** [api.sshnk.com](https://api.sshnk.com)

---

## Key Features

- 🏠 **Room Workspaces**: Create multi-member household rooms, invite roommates via codes or shareable invite links, and manage member approvals.
- 📦 **Container Inventory**: Track shared supplies (eggs, milk, household essentials) with live stock levels, unit metrics, and container lifecycle controls.
- ⚖️ **Expense & Debt Settlement**: Automated "who-owes-whom" balance matrix calculation with one-click debt settlement logs.
- 📈 **Activity Stream & Streaks**: Real-time room activity feed, order histories, and gamified member consumption streaks.
- 🔐 **Hardened Security Architecture**:
  - Access tokens stored strictly **in memory**; refresh tokens stored in **HttpOnly, Secure, SameSite** cookies.
  - Silent token rotation with custom anti-CSRF protection (`X-EggLedger-CSRF`).
  - Google OAuth 2.0 via a confidential server-side authorization-code flow.
  - Configurable IP rate limiting across global and auth-sensitive endpoints.
- 🌗 **Adaptive UI**: Responsive interface optimized for desktop and mobile, with full Dark and Light theme support.
- 🔍 **Interactive API Documentation**: Embedded OpenAPI documentation via Scalar in development.

---

## Tech Stack

| Domain | Technology |
| --- | --- |
| **Backend API** | ASP.NET Core (.NET 10), Entity Framework Core, FluentResults |
| **Frontend SPA** | Vue 3 (Composition API), Vite, Pinia, Vue Router, Axios |
| **Database** | PostgreSQL 15+ |
| **Dev Orchestration** | .NET Aspire (PostgreSQL container + API + Vite SPA + pgWeb) |
| **Authentication** | JWT (access tokens) + HttpOnly cookies (refresh) + Google OAuth 2.0 |
| **Testing** | xUnit, Testcontainers, WebApplicationFactory |
| **Observability** | OpenTelemetry → Azure Monitor / Application Insights (prod) & Aspire dashboard (dev) |
| **Production Hosting** | Azure Container Apps (API) + Azure Static Web Apps (SPA) + PostgreSQL Flexible Server |

---

## Architecture

The backend adheres to a strict, layered separation of concerns:

```text
EggLedger/
├── EggLedger.API/              # Controllers (thin, no business logic), middleware, DI/config wiring
├── EggLedger.Services/         # Domain & business logic (returns Result<T> via FluentResults)
├── EggLedger.Data/             # DbContext, EF Core mappings, and database migrations
├── EggLedger.Models/           # Database-mapped domain entities
├── EggLedger.DTO/              # Strongly-typed API request/response contracts
├── EggLedger.ServiceDefaults/  # Cross-cutting Aspire defaults: health checks, OpenTelemetry
├── EggLedger.AppHost/          # .NET Aspire orchestration for local development
├── EggLedger.Client/           # Vue 3 Single Page Application
└── EggLedger.Tests/            # xUnit integration test suite using Testcontainers
```

API endpoints are organized under the `/egg-ledger-api/` route prefix. Controllers validate inputs and delegate immediately to domain services; entities are never exposed directly to consumers.

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) `^20.19.0` or `>=22.12.0`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local database containers)

### Option 1: .NET Aspire (Recommended)

.NET Aspire spins up the entire development environment — including PostgreSQL, pgWeb database manager, the backend API, and the Vite frontend dev server — with a unified observability dashboard:

```bash
git clone https://github.com/sheshankchandra/EggLedger.git
cd EggLedger

# Install frontend dependencies
cd EggLedger.Client && npm install && cd ..

# Start the full stack
dotnet run --project EggLedger.AppHost
```

Open the **Aspire Dashboard** at the URL printed in the terminal (typically `https://localhost:17071`) to launch the frontend, inspect API traces and logs, and browse the database via pgWeb.

### Option 2: Docker Compose

To launch PostgreSQL, pgAdmin, the API, and the Client as standalone containers:

```bash
docker-compose up -d
```

- **Client**: `http://localhost:5173`
- **API**: `http://localhost:8080`
- **pgAdmin**: `http://localhost:5050` (`admin@eggledger.com` / `eggledger123`)

---

## Configuration & Secrets

Local development uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep credentials out of version control. The committed configuration templates (`appsettings.json`, `appsettings-example.json`) contain only non-sensitive defaults.

Key application configuration entries:

```jsonc
{
  "Jwt": {
    "SecretKey": "<secure-random-key-at-least-32-chars>",
    "Issuer": "EggLedgerAPI",
    "Audience": "EggLedgerAudience",
    "ExpiryInMinutes": 15
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=eggledgerDB;Username=...;Password=..."
  },
  "Authentication": {
    "Google": {
      "ClientId": "<google-client-id>",
      "ClientSecret": "<google-client-secret>"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  },
  "Ef_Migrate": "true" // Automatically runs EF migrations on startup in development
}
```

Detailed guides:
- [Secrets & Configuration Guide](docs/SECRETS.md) — User Secrets setup, environment variable mapping, and Key Vault integration.
- [Database Migrations Guide](docs/MIGRATIONS.md) — EF Core migration workflows and production deployment procedures.

---

## API Overview

Interactive Scalar API documentation is available at `/scalar/v2` when running in development mode.

Primary API route groups:
- **Authentication**: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `GET /auth/google-login`
- **Rooms**: `POST /room/create`, `POST /room/join`, `GET /room/user/all`, `GET /room/{roomCode}`
- **Containers**: `GET /room/{roomCode}/container/all`, `POST /room/{roomCode}/container/create`, `PATCH /room/{roomCode}/container/{id}`
- **Orders & Inventory**: `POST /{roomCode}/orders/stock`, `POST /{roomCode}/orders/consume`
- **Settlement & Ledgers**: `GET /room/{roomCode}/balances`, `POST /room/{roomCode}/settle`
- **Activity**: `GET /room/{roomCode}/activity`

---

## Testing

EggLedger includes an integration test suite powered by **xUnit**, **ASP.NET Core WebApplicationFactory**, and **Testcontainers**:

```bash
# Ensure Docker is running, then execute:
dotnet test
```

The test harness spins up an ephemeral PostgreSQL container to validate authentication lifecycles, refresh cookie rotation, anti-CSRF enforcement, and rate-limiting rules end-to-end.

---

## Production Deployment

EggLedger is designed for modern cloud hosting on Microsoft Azure:
- **Frontend SPA**: Hosted on [Azure Static Web Apps](https://azure.microsoft.com/services/app-service/static/) with automated CI/CD via GitHub Actions.
- **Backend API**: Deployed to [Azure Container Apps](https://azure.microsoft.com/services/container-apps/) (scale-to-zero serverless container hosting).
- **Database**: [Azure Database for PostgreSQL](https://azure.microsoft.com/services/postgresql/) Flexible Server.
- **Security**: Managed Identity with [Azure Key Vault](https://azure.microsoft.com/services/key-vault/) for zero-secret manifests.

For complete provisioning commands and network topology, consult the [Production Deployment Guide](docs/DEPLOYMENT.md).

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on code style, architectural conventions, and the pull request process.

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
