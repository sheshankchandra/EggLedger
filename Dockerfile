# Root Dockerfile for EggLedger Solution
# This Dockerfile builds the entire solution and can be used for production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY EggLedger.sln ./

# Copy all project files
COPY EggLedger.API/*.csproj ./EggLedger.API/
COPY EggLedger.AppHost/*.csproj ./EggLedger.AppHost/
COPY EggLedger.Data/*.csproj ./EggLedger.Data/
COPY EggLedger.DTO/*.csproj ./EggLedger.DTO/
COPY EggLedger.Models/*.csproj ./EggLedger.Models/
COPY EggLedger.ServiceDefaults/*.csproj ./EggLedger.ServiceDefaults/
COPY EggLedger.Services/*.csproj ./EggLedger.Services/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the solution
RUN dotnet build -c Release --no-restore

# Publish the API
FROM build AS publish-api
WORKDIR /src/EggLedger.API
RUN dotnet publish -c Release -o /app/api --no-build

# Publish the AppHost
FROM build AS publish-apphost
WORKDIR /src/EggLedger.AppHost
RUN dotnet publish -c Release -o /app/apphost --no-build

# Final stage for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api
WORKDIR /app
COPY --from=publish-api /app/api .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "EggLedger.API.dll"]

# Final stage for AppHost
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS apphost
WORKDIR /app
COPY --from=publish-apphost /app/apphost .
EXPOSE 15000
EXPOSE 15001
ENTRYPOINT ["dotnet", "EggLedger.AppHost.dll"]
