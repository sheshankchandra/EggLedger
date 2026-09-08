# Database Migrations Guide

EggLedger uses Entity Framework Core (EF Core) migrations to manage its PostgreSQL database schema.

> [!IMPORTANT]
> **Development auto-applies migrations for convenience; Production does not.**
> In production environments, migrations are reviewed and applied out-of-band as part of the deployment pipeline.

---

## Strategy: Dev vs. Production

The API includes an optional startup migration runner (`MiddlewareExtensions.HandleDatabaseMigrationAsync`) controlled by the `Ef_Migrate` configuration setting:

- **Development (`Ef_Migrate=true`)**: Automatically applies any pending migrations when the API boots, enabling fast local iteration.
- **Production (`Ef_Migrate=false`)**: Automatic migration on startup is strictly disabled.

### Why Production Avoids In-App Migrations

1. **Race Conditions**: When multiple container replicas start up simultaneously, concurrent migration runs can conflict and corrupt migration history.
2. **Safety & Auditability**: Production schema updates should be generated as reviewable SQL scripts, inspected, and backed up before execution.
3. **Principle of Least Privilege**: The production application runtime needs only Data Manipulation (CRUD) permissions, not Data Definition (DDL) privileges.
4. **Startup Timeouts & Coupling**: Large migrations can block container startup, causing platform health check timeouts and restart loops.

---

## Development Workflow

### Creating a Migration

When modifying entity models in `EggLedger.Models`:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project EggLedger.Data --startup-project EggLedger.API
```

### Applying Migrations Locally

When running locally without Aspire or when applying updates manually:

```bash
# Apply pending migrations to the local database
dotnet ef database update --project EggLedger.Data --startup-project EggLedger.API
```

### Rolling Back Locally

To revert the most recent migration on your local database:

```bash
# Roll back database to a previous migration
dotnet ef database update <PreviousMigrationName> --project EggLedger.Data --startup-project EggLedger.API

# Remove the scaffolded migration files
dotnet ef migrations remove --project EggLedger.Data --startup-project EggLedger.API
```

---

## Production Workflow

### 1. Generate Idempotent SQL Script

Generate a migration script that checks the `__EFMigrationsHistory` table before executing each migration step. This makes the script safe to run repeatedly:

```bash
dotnet ef migrations script --idempotent \
  --project EggLedger.Data --startup-project EggLedger.API \
  -o migrate.sql
```

### 2. Review and Apply

Review the generated SQL script, then apply it against the target production database using a DDL-privileged connection string:

```bash
psql "<production-connection-string>" -f migrate.sql
```

This step runs out-of-band before activating new application revisions, ensuring zero downtime and preventing startup failures.

---

## Concurrency Note

If an automated multi-replica migration strategy is ever required, serialize execution using PostgreSQL advisory locks (`pg_advisory_lock`) to ensure only one instance executes migrations while others wait. The recommended out-of-band script workflow avoids this complexity entirely.
