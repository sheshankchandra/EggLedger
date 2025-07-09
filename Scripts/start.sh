#!/bin/bash

# Start production environment
echo "Starting EggLedger Production Environment..."

# Stop any running containers
docker-compose -f docker-compose.prod.yml down

# Build and start production containers in detached mode
docker-compose -f docker-compose.prod.yml up --build -d

echo ""
echo "Production environment started successfully!"
echo "API: http://localhost:8080"
echo "Client: http://localhost:5173"
echo "Database: localhost:5432"
echo ""
echo "To view logs: docker-compose -f docker-compose.prod.yml logs -f"
echo "To stop: docker-compose -f docker-compose.prod.yml down"
