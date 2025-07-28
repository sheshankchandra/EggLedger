#!/bin/bash

# Start Environment
echo "Executing script..."

# Stop any running containers and remove volumes
# TIP: Comment out the line below to PERSIST your data between restarts
# docker-compose -f docker-compose.yml down -v
echo "Stopping any running containers and removing volumes..."
docker-compose -f docker-compose.yml down -v

# OPTIMIZATION STRATEGY: Build and start database first
# This allows the database to initialize while application images are being built
# This parallel processing reduces overall startup time significantly
echo "Building database image..."
docker-compose -f docker-compose.yml build postgres --no-cache

# Start Database container (runs in background while we build other images)
echo "Starting Database container..."
docker-compose -f docker-compose.yml up -d postgres

# Build all remaining images (API and Client)
echo "Building API and Client..."
docker-compose -f docker-compose.yml build --no-cache

# Start API and Client
echo "Starting API and Client..."
docker-compose -f docker-compose.yml up -d

echo ""
echo "EggLedger started successfully!"
echo "API: http://localhost:8080"
echo "Client: http://localhost:5173"
echo "Database: localhost:5432"
echo "pgAdmin: http://localhost:5050 (admin@eggledger.com / eggledger123)"
echo ""
echo "To view logs: docker-compose -f docker-compose.yml logs -f"
echo "To stop: docker-compose -f docker-compose.yml down"
echo ""
echo "💡 TIP: To persist data between restarts, comment out 'docker-compose down -v' above"
echo "💡 TIP: Use 'docker-compose down' (without -v) to stop but keep data"
echo "To rebuild: docker-compose build --no-cache"
echo "To reset database: docker-compose down -v"