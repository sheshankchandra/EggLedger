# Start production environment
Write-Host "Starting EggLedger Production Environment..." -ForegroundColor Green

# Stop any running containers
docker-compose -f docker-compose.prod.yml down

# Build and start production containers
docker-compose -f docker-compose.prod.yml up --build -d

Write-Host "Production environment started in detached mode." -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Client: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Database: localhost:5432" -ForegroundColor Cyan
