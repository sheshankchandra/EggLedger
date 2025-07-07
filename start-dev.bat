@echo off
echo Starting EggLedger Development Environment...

rem Stop any running containers
docker-compose -f docker-compose.dev.yml down

rem Build and start development containers
docker-compose -f docker-compose.dev.yml up --build

echo Development environment stopped.
