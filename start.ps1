# Start production environment
Write-Host "Starting EggLedger Environment..." -ForegroundColor Green

# Stop any running containers
docker-compose down -v

# Build Database
Write-Host "Building database image..." -ForegroundColor Yellow
docker-compose build postgres --no-cache

# Start Database container
Write-Host "Starting Database container..." -ForegroundColor Yellow
docker-compose up -d postgres

# Build all remaining images
Write-Host "Building all remaining images..." -ForegroundColor Yellow
docker-compose build --no-cache

# Start all application services
Write-Host "Starting application services..." -ForegroundColor Yellow
docker-compose up -d

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