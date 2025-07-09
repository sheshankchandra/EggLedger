# Start production environment
Write-Host "Starting EggLedger Environment..." -ForegroundColor Green

# Stop any running containers
docker-compose down

# Start only the database
Write-Host "Starting database..." -ForegroundColor Yellow
docker-compose up -d postgres

# Wait for database to be ready
Write-Host "Waiting for database to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10
do {
    $dbReady = docker-compose exec postgres pg_isready -U eggledger -d eggledgerDB
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Database is unavailable - sleeping" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
} while ($LASTEXITCODE -ne 0)
Write-Host "Database is ready!" -ForegroundColor Green

# Run migrations locally
Write-Host "Running EF Core migrations..." -ForegroundColor Yellow
dotnet ef database update --project EggLedger.API
if ($LASTEXITCODE -ne 0) {
    Write-Host "Migration failed!" -ForegroundColor Red
    exit 1
}

# Build and start production containers
Write-Host "Starting application services..." -ForegroundColor Yellow
docker-compose up -d api client

Write-Host ""
Write-Host "Production environment started successfully!" -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Client: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Database: localhost:5432" -ForegroundColor Cyan
Write-Host ""
Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Yellow
Write-Host "To stop: docker-compose down" -ForegroundColor Yellow
Write-Host "To rebuild: docker-compose build --no-cache" -ForegroundColor Yellow
Write-Host "To reset database: docker-compose down -v" -ForegroundColor Yellow