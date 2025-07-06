# Dockerfile specifically for EggLedger.AppHost (Aspire)

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY EggLedger.sln ./

# Copy project files needed for AppHost
COPY EggLedger.AppHost/*.csproj ./EggLedger.AppHost/
COPY EggLedger.API/*.csproj ./EggLedger.API/
COPY EggLedger.ServiceDefaults/*.csproj ./EggLedger.ServiceDefaults/

# Restore dependencies
RUN dotnet restore EggLedger.AppHost/EggLedger.AppHost.csproj

# Copy source code
COPY EggLedger.AppHost/ ./EggLedger.AppHost/
COPY EggLedger.ServiceDefaults/ ./EggLedger.ServiceDefaults/

# Build the AppHost
WORKDIR /src/EggLedger.AppHost
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install required dependencies for Aspire
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Expose Aspire dashboard ports
EXPOSE 15000
EXPOSE 15001

ENTRYPOINT ["dotnet", "EggLedger.AppHost.dll"]
