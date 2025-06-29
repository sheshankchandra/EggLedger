# Database Connection Error Handling

## Overview

This application now includes comprehensive database connection error handling to gracefully handle scenarios where PostgreSQL is not running or unavailable.

## Features Added

### 1. **Global Exception Handling Middleware**
- **File**: `EggLedger.API/Middleware/GlobalExceptionHandlingMiddleware.cs`
- **Purpose**: Catches all unhandled exceptions and provides user-friendly error messages
- **Database Errors**: Specifically handles `NpgsqlException` with clear messaging

**Example Response when database is down**:
```json
{
  "error": "Database service is currently unavailable. Please ensure the database server is running and try again.",
  "details": "Please contact your administrator or check if the database service is running.",
  "timestamp": "2025-06-29T10:30:00.000Z",
  "statusCode": 503
}
```

### 2. **Database Connection Retry Policy**
- **File**: `EggLedger.API/Program.cs` (line 29-35)
- **Features**:
  - Automatic retry on connection failures (3 retries)
  - 5-second delay between retries
  - Built-in resilience for transient failures

### 3. **Health Check Endpoints**
- **Endpoints**:
  - `GET /health` - Overall application health (ASP.NET Core built-in)
  - `GET /health/ready` - Readiness probe for deployment  
  - `GET /egg-ledger-api/health` - Detailed health status
  - `GET /egg-ledger-api/health/database` - Database-specific health check

**Example Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-06-29T10:30:00.000Z",
  "services": {
    "database": {
      "status": "Connected",
      "canConnect": true
    }
  }
}
```

### 4. **Startup Database Validation**
- **File**: `EggLedger.API/Services/DatabaseStartupValidationService.cs`
- **Purpose**: Validates database connection on application startup
- **Behavior**: Logs warnings if database is unavailable but doesn't prevent startup

**Console Output Examples**:
```
✅ Database connection validated successfully
```
or
```
⚠️ Database is not available on startup. The application will continue, but database-dependent features may fail.
```

### 5. **Database Service**
- **File**: `EggLedger.API/Services/DatabaseService.cs`
- **Purpose**: Provides reusable database connectivity checks
- **Methods**:
  - `IsAvailableAsync()` - Quick availability check
  - `CanConnectAsync()` - Full connection test

## Usage Examples

### Starting the Application Without Database

1. **Start the API without PostgreSQL running**:
   ```bash
   cd EggLedger.API
   dotnet run
   ```

2. **Expected Console Output**:
   ```
   info: EggLedger.API.Middleware.DatabaseStartupValidationService[0]
         Validating database connection on startup...
   warn: EggLedger.API.Middleware.DatabaseStartupValidationService[0]
         ⚠️ Database is not available on startup. The application will continue, but database-dependent features may fail.
   warn: EggLedger.API.Middleware.DatabaseStartupValidationService[0]
         Please ensure PostgreSQL is running and the connection string is correct.
   ```

3. **API Response for any database-dependent endpoint**:
   ```json
   {
     "error": "Database service is currently unavailable. Please ensure the database server is running and try again.",
     "details": "Please contact your administrator or check if the database service is running.",
     "timestamp": "2025-06-29T10:30:00.000Z",
     "statusCode": 503
   }
   ```

### Health Check Usage

1. **Check overall health**:
   ```bash
   curl https://localhost:7224/health
   ```

2. **Check database specifically**:
   ```bash
   curl https://localhost:7224/egg-ledger-api/health/database
   ```

## Configuration

### Connection String
Update in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Username=asapdb;Password=asap;Database=EggLedgerDB"
  }
}
```

### Retry Policy Settings
The retry policy is configured in `Program.cs` and can be adjusted:
```csharp
npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount: 3,        // Number of retries
    maxRetryDelay: TimeSpan.FromSeconds(5),  // Max delay between retries
    errorCodesToAdd: null    // Additional error codes to retry
);
```

## Benefits

1. **Better User Experience**: Clear error messages instead of technical stack traces
2. **Graceful Degradation**: Application starts even if database is down
3. **Monitoring Ready**: Health checks for deployment and monitoring tools
4. **Development Friendly**: Developers get clear feedback about database issues
5. **Production Ready**: Proper error handling and logging for production environments

## Troubleshooting

### Common Issues

1. **PostgreSQL Not Running**:
   - **Error**: `Failed to connect to 127.0.0.1:5432`
   - **Solution**: Start PostgreSQL service or Docker container

2. **Wrong Connection String**:
   - **Error**: Various connection-related errors
   - **Solution**: Verify connection string in `appsettings.json`

3. **Database Doesn't Exist**:
   - **Error**: Database "EggLedgerDB" does not exist
   - **Solution**: Run database migrations: `dotnet ef database update`

### Docker PostgreSQL Quick Start
```bash
docker run --name eggledger-postgres -e POSTGRES_USER=asapdb -e POSTGRES_PASSWORD=asap -e POSTGRES_DB=EggLedgerDB -p 5432:5432 -d postgres:13
```

## Testing the Error Handling

1. **Stop PostgreSQL** (if running)
2. **Start the API**: `dotnet run`
3. **Make any API request** to a database-dependent endpoint
4. **Observe**: Graceful error response instead of exception
5. **Check health endpoint**: `/api/health/database` shows database status

This implementation ensures your application handles database connection issues gracefully while providing clear feedback to users and administrators.
