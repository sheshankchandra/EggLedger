# Production Dockerfile for EggLedger API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency restoration
COPY ["EggLedger.API/EggLedger.API.csproj", "EggLedger.API/"]
COPY ["EggLedger.Data/EggLedger.Data.csproj", "EggLedger.Data/"]
COPY ["EggLedger.Models/EggLedger.Models.csproj", "EggLedger.Models/"]
COPY ["EggLedger.DTO/EggLedger.DTO.csproj", "EggLedger.DTO/"]
COPY ["EggLedger.ServiceDefaults/EggLedger.ServiceDefaults.csproj", "EggLedger.ServiceDefaults/"]
COPY ["EggLedger.Services/EggLedger.Services.csproj", "EggLedger.Services/"]

# Restore dependencies
RUN dotnet restore "EggLedger.API/EggLedger.API.csproj"

# Copy all source code
COPY . .

WORKDIR /src/EggLedger.API

# Build the application
RUN dotnet build "EggLedger.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EggLedger.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "EggLedger.API.dll"]