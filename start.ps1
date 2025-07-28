# Start Environment
Write-Host "Executing script..." -ForegroundColor Green

# Stop any running containers and remove volumes
# TIP: Comment out the line below to PERSIST your data between restarts
# docker-compose down -v
Write-Host "Stopping any running containers and removing volumes..." -ForegroundColor Yellow
docker-compose down -v

# OPTIMIZATION STRATEGY: Build and start database first
# This allows the database to initialize while application images are being built
# This parallel processing reduces overall startup time significantly
Write-Host "Building database image..." -ForegroundColor Yellow
docker-compose build postgres --no-cache

# Start Database container (runs in background while we build other images)
Write-Host "Starting Database container..." -ForegroundColor Yellow
docker-compose up -d postgres

# Build all remaining images (API and Client)
# Database is already running, so applications can connect immediately when ready
Write-Host "Building API and Client..." -ForegroundColor Yellow
docker-compose build --no-cache

# Start all application services
Write-Host "Starting API and Client..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "EggLedger started successfully!" -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Client: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Database: localhost:5432" -ForegroundColor Cyan
Write-Host "pgAdmin: http://localhost:5050 (admin@eggledger.com / eggledger123)" -ForegroundColor Cyan
Write-Host ""
Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Yellow
Write-Host "To stop: docker-compose down" -ForegroundColor Yellow
Write-Host "To rebuild: docker-compose build --no-cache" -ForegroundColor Yellow
Write-Host "To reset database: docker-compose down -v" -ForegroundColor Yellow
Write-Host ""
Write-Host "💡 TIP: To persist data between restarts, comment out 'docker-compose down -v' above" -ForegroundColor Magenta
Write-Host "💡 TIP: Use 'docker-compose down' (without -v) to stop but keep data" -ForegroundColor Magenta