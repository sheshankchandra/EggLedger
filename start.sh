#!/bin/bash

# Start production environment
echo "Starting EggLedger Environment..."

# Stop any running containers and remove volumes
docker-compose -f docker-compose.yml down -v

# Build Database
echo "Building database image..."
docker-compose -f docker-compose.yml build postgres --no-cache

# Start Database container
echo "Starting Database container..."
docker-compose -f docker-compose.yml up -d postgres

# Build all remaining images
echo "Building all remaining images..."
docker-compose -f docker-compose.yml build --no-cache

# Start all application services
echo "Starting application services..."
docker-compose -f docker-compose.yml up -d

echo ""
echo "Production environment started successfully!"
echo "API: http://localhost:8080"
echo "Client: http://localhost:5173"
echo "Database: localhost:5432"
echo ""
echo "To view logs: docker-compose -f docker-compose.yml logs -f"
echo "To stop: docker-compose -f docker-compose.yml down"