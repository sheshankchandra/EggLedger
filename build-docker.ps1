# Build script for EggLedger Docker containers
# This PowerShell script builds all Docker images for the EggLedger solution

param(
    [string]$Environment = "Production",
    [switch]$NoBuild = $false,
    [switch]$Push = $false,
    [string]$Registry = "",
    [string]$Tag = "latest"
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "Building EggLedger Docker containers..." -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Tag: $Tag" -ForegroundColor Yellow

# Define image names
$images = @{
    "api" = "eggledger-api"
    "client" = "eggledger-client"
    "apphost" = "eggledger-apphost"
    "solution" = "eggledger-solution"
}

# Add registry prefix if provided
if ($Registry) {
    $images = $images.GetEnumerator() | ForEach-Object {
        @{ $_.Key = "$Registry/$($_.Value)" }
    } | ForEach-Object { $_ }
}

try {
    if (-not $NoBuild) {
        Write-Host "Building API image..." -ForegroundColor Cyan
        docker build -f EggLedger.API/Dockerfile -t "$($images.api):$Tag" .
        
        Write-Host "Building Client image..." -ForegroundColor Cyan
        docker build -f EggLedger.Client/Dockerfile -t "$($images.client):$Tag" ./EggLedger.Client
        
        Write-Host "Building AppHost image..." -ForegroundColor Cyan
        docker build -f EggLedger.AppHost/Dockerfile -t "$($images.apphost):$Tag" .
        
        Write-Host "Building Solution image..." -ForegroundColor Cyan
        docker build -f Dockerfile -t "$($images.solution):$Tag" --target api .
    }
    
    # List built images
    Write-Host "Built images:" -ForegroundColor Green
    foreach ($image in $images.Values) {
        docker images --filter "reference=$image" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
    }
    
    # Push images if requested
    if ($Push) {
        Write-Host "Pushing images to registry..." -ForegroundColor Magenta
        foreach ($image in $images.Values) {
            Write-Host "Pushing $image`:$Tag..." -ForegroundColor Yellow
            docker push "$image`:$Tag"
        }
    }
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    exit 1
}

# Display usage instructions
Write-Host "`nUsage Instructions:" -ForegroundColor Yellow
Write-Host "1. Start with development environment:" -ForegroundColor White
Write-Host "   docker-compose -f docker-compose.dev.yml up -d" -ForegroundColor Gray
Write-Host "`n2. Start production environment:" -ForegroundColor White
Write-Host "   docker-compose up -d" -ForegroundColor Gray
Write-Host "`n3. View logs:" -ForegroundColor White
Write-Host "   docker-compose logs -f [service-name]" -ForegroundColor Gray
Write-Host "`n4. Stop services:" -ForegroundColor White
Write-Host "   docker-compose down" -ForegroundColor Gray
