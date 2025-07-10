#!/bin/bash

# Start production environment
echo "Starting EggLedger Environment..."

# Stop any running containers
docker-compose -f docker-compose.yml down

# Start only the database
echo "Starting database..."
docker-compose up -d postgres

# Wait for database to be ready
echo "Waiting for database to be ready..."
sleep 10
until docker-compose exec postgres pg_isready -U eggledger -d eggledgerDB; do
  echo "Database is unavailable - sleeping"
  sleep 2
done
echo "Database is ready!"

# Run migrations locally
echo "Running EF Core migrations..."
dotnet ef database update --project EggLedger.API

# Build and start production containers in detached mode
echo "Starting application services..."
docker-compose -f docker-compose.yml up --build -d

echo ""
echo "Production environment started successfully!"
echo "API: http://localhost:8080"
echo "Client: http://localhost:5173"
echo "Database: localhost:5432"
echo ""
echo "To view logs: docker-compose -f docker-compose.yml logs -f"
echo "To stop: docker-compose -f docker-compose.yml down"