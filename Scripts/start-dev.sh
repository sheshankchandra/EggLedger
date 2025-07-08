#!/bin/bash

# Start development environment
echo "Starting EggLedger Development Environment..."

# Stop any running containers
docker-compose -f docker-compose.dev.yml down

# Build and start development containers
docker-compose -f docker-compose.dev.yml up --build

echo "Development environment stopped."
