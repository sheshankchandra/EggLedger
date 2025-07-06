@echo off
REM Build script for EggLedger Docker containers (Batch version)

echo Building EggLedger Docker containers...

REM Build API image
echo Building API image...
docker build -f EggLedger.API/Dockerfile -t eggledger-api:latest .
if %ERRORLEVEL% neq 0 (
    echo Failed to build API image
    exit /b 1
)

REM Build Client image
echo Building Client image...
docker build -f EggLedger.Client/Dockerfile -t eggledger-client:latest ./EggLedger.Client
if %ERRORLEVEL% neq 0 (
    echo Failed to build Client image
    exit /b 1
)

REM Build AppHost image
echo Building AppHost image...
docker build -f EggLedger.AppHost/Dockerfile -t eggledger-apphost:latest .
if %ERRORLEVEL% neq 0 (
    echo Failed to build AppHost image
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo Usage Instructions:
echo 1. Start development environment: docker-compose -f docker-compose.dev.yml up -d
echo 2. Start production environment: docker-compose up -d
echo 3. View logs: docker-compose logs -f [service-name]
echo 4. Stop services: docker-compose down
