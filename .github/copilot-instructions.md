# EggLedger — Copilot Instructions

These instructions are automatically provided to GitHub Copilot for every request in this
repository. Keep them short, specific, and current.

## Project Overview

EggLedger is a full-stack roommate resource & expense management app. Users create rooms,
track shared groceries/supplies (containers), and record stock/consumption orders with
automatic balance calculations.

**Stack:** ASP.NET Core (.NET 9) Web API · Vue 3 + Vite + Pinia SPA · PostgreSQL 15 ·
Entity Framework Core · .NET Aspire (dev orchestration) · JWT + Google OAuth 2.0.

## Architecture — where code goes

The backend is layered. Respect these boundaries:

- **EggLedger.API** — Controllers (thin), middleware, DI/config wiring, `Program.cs`,
  extension methods (`Extensions/`). Controllers validate input and delegate; they contain
  **no business logic**.
- **EggLedger.Services** — All business logic. Services return `FluentResults` results, not
  raw exceptions, for expected failures.
- **EggLedger.Data** — `DbContext`, EF Core configuration, migrations.
- **EggLedger.Models** — Domain entities (database-mapped).
- **EggLedger.DTO** — Request/response shapes (Room DTOs live in `EggLedger.DTO/Room/`).
  **Never** return EF entities directly from the API; map to a DTO. (Note: the top-level
  `EggLedger.DTO.Room/` folder is orphaned duplicate code, not part of the build — do not
  add to it.)
- **EggLedger.ServiceDefaults** — Shared Aspire defaults (health checks, OpenTelemetry).
- **EggLedger.AppHost** — Aspire orchestration. **Development only** — do not containerize
  or deploy the AppHost itself.
- **EggLedger.Client** — Vue 3 SPA.

API routes are prefixed with `/egg-ledger-api/`. API docs are served via Scalar at
`/scalar/v2` in development.

## C# conventions

- Shared build settings live in root `Directory.Build.props`; **all** NuGet versions live in
  root `Directory.Packages.props` (Central Package Management). Add packages with
  `<PackageReference Include="X" />` (no `Version=`) and pin the version in
  `Directory.Packages.props`.
- `Nullable` and `ImplicitUsings` are both **enabled** solution-wide (do not redeclare them
  in individual `.csproj` files). Common `using`s are implicit; add only the extra ones.
- Use **file-scoped namespaces** (`namespace X;`), sorting `System.*` usings first. Enforced
  by `.editorconfig`; run `dotnet format` to normalize.
- .NET analyzers are on (`AnalysisLevel latest-recommended`). Fix warnings rather than
  suppressing; tune rule severity centrally in `.editorconfig` if a rule is noisy.
- Prefer `async`/`await` end to end; suffix async methods with `Async`.
- Use the injected `ILogger<T>` for logging — **never** `Console.WriteLine`.
- Validate DTOs with data annotations (`[Required]`, `[MinLength]`, `[EmailAddress]`, etc.).
- Return `FluentResults` from services; translate to appropriate HTTP status codes in the
  controller.

## Vue / frontend conventions

- Vue 3 **Composition API** with `<script setup>`; state in **Pinia** stores (`src/stores`).
- API access goes through the axios client in `src/services` — do not call `fetch` ad hoc.
- The API base URL must come from `import.meta.env.VITE_API_BASE_URL` (env-based), never
  hardcoded.
- Formatting is enforced by Prettier: **no semicolons, single quotes, printWidth 100**.
  Lint with `npm run lint`, format with `npm run format`.

## Security rules (important — the app is going to production)

- **Never** commit secrets. `appsettings.json`, `.env`, and `.env.*` are git-ignored; keep
  real secrets in user-secrets (dev) or the platform secret store (prod). Only
  `appsettings-example.json` with placeholders is committed.
- **Never** put tokens (JWT/refresh) in URLs or query strings.
- Keep CORS restricted to configured origins; do not use `AllowAnyOrigin`.
- Validate and bound all user input; paginate list endpoints.

## Build & run

```bash
# Full stack (Aspire) — starts Postgres, API, and the Vue dev server
dotnet run --project EggLedger.AppHost

# Frontend only
cd EggLedger.Client && npm install && npm run dev

# Build / format the whole solution
dotnet build EggLedger.sln
dotnet format EggLedger.sln
```

> **Tests:** there is currently **no** automated test project (despite older README text).
> A test suite is being added — do not assume `dotnet test` passes yet.

## When making changes

- Match the existing layering and conventions above.
- Add/adjust input validation and tests for behavior you change.
- Do not introduce secrets, and do not weaken CORS or auth.
