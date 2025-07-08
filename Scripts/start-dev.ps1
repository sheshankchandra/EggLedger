# Start development environment
Write-Host "Starting EggLedger Development Environment..." -ForegroundColor Green

# Stop any running containers
docker-compose -f docker-compose.dev.yml down

# Build and start development containers
docker-compose -f docker-compose.dev.yml up --build

Write-Host "Development environment stopped." -ForegroundColor Yellow
