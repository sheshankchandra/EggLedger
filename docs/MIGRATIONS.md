# Database Migrations

EggLedger uses EF Core migrations. The key rule:

> **Development auto-applies migrations for convenience. Production does NOT.**
> In production, migrations are a deliberate, reviewed step run out-of-band.

## Why production does not auto-migrate

The API can auto-run migrations on startup when `Ef_Migrate=true`
(`MiddlewareExtensions.HandleDatabaseMigrationAsync`). This is enabled in
development but **disabled in `appsettings.Production.json`** (`Ef_Migrate=false`)
because auto-migrating a production database on startup is risky:

- **Races** — multiple Container Apps replicas starting together migrate the same DB concurrently.
- **No safety net** — a bad migration runs automatically before review or backup.
- **Privilege creep** — the runtime identity would need permanent DDL rights instead of least-privilege CRUD.
- **Startup coupling** — a long migration blocks health checks and the platform kills the app mid-migration.

## Everyday development

Auto-migrate is on in development, so migrations apply when the API starts.
To work with migrations manually:

```powershell
# Add a migration after changing the model
dotnet ef migrations add <Name> --project EggLedger.Data --startup-project EggLedger.API

# Apply pending migrations to the local dev database
dotnet ef database update --project EggLedger.Data --startup-project EggLedger.API
```

## Production — deliberate, reviewed apply

Generate an **idempotent** SQL script (safe to run repeatedly; each migration is
guarded by a check), review it, then apply it out-of-band at deploy time:

```powershell
dotnet ef migrations script --idempotent `
  --project EggLedger.Data --startup-project EggLedger.API `
  -o migrate.sql
```

Apply the reviewed script against the production database with a **DDL-capable**
admin credential (separate from the runtime app's least-privilege connection):

```powershell
psql "<admin connection string>" -f migrate.sql
```

This will be wired into the Phase 3 deployment as an explicit step that runs
*before* the new API revision goes live — never inside app startup.

## If you ever must auto-migrate with multiple replicas

Don't, if avoidable. If unavoidable, serialize with a PostgreSQL advisory lock
(`pg_advisory_lock`) so only one instance migrates while the others wait, then
re-check for pending migrations. The out-of-band script approach above avoids
this problem entirely.
