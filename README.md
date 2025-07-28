# EggLedger

A full-stack roommate resource and expense management application built with ASP.NET Core and Vue.js. Track shared groceries, supplies, and household expenses with room-based organization and automatic balance calculations.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Vue.js](https://img.shields.io/badge/Vue.js-3.x-green.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)

## Features

- **Room-based organization** with unique room codes
- **Resource tracking** for shared items and expenses
- **User authentication** with JWT and Google OAuth
- **Real-time updates** for stock levels and consumption
- **Role-based permissions** for room admins and members
- **Database management** with pgAdmin interface

## Technology Stack

**Backend:** ASP.NET Core 9.0, Entity Framework Core, PostgreSQL 15  
**Frontend:** Vue.js 3, Vite, Pinia  
**Authentication:** JWT, Google OAuth 2.0  
**Orchestration:** .NET Aspire 9.3 (development), Docker Compose (production)  
**Database Management:** pgAdmin 4  
**Monitoring:** Built-in health checks, OpenTelemetry  
**Documentation:** OpenAPI/Swagger

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Option 1: Using .NET Aspire (Development - Recommended)

.NET Aspire provides orchestration for the entire application stack including PostgreSQL, API, and frontend.

```bash
# Clone the repository
git clone https://github.com/sheshankchandra/EggLedger.git
cd EggLedger

# Restore all packages
dotnet restore

# Install frontend dependencies
cd EggLedger.Client
npm install
cd ..

# Run the entire application with Aspire
dotnet run --project EggLedger.AppHost
```

This will:
- Start PostgreSQL 15 in a Docker container
- Run the API server with proper database connection
- Start the Vue.js development server
- Provide a unified dashboard at `https://localhost:17071`
- Include pgAdmin for database management (accessible via Aspire dashboard)

### Option 2: Using Docker Compose

For containerized deployment (development, staging, or production) when you want to run all services in containers:

```bash
# Clone the repository
git clone https://github.com/sheshankchandra/EggLedger.git
cd EggLedger

# Copy and configure the appsettings
cp EggLedger.API/appsettings-example.json EggLedger.API/appsettings.json

# Edit appsettings.json with your configuration
# (See Configuration section below)

# Start all services
docker-compose up -d
```

This will start:
- **PostgreSQL 15** on port `5432`
- **pgAdmin 4** on port `5050` (admin@eggledger.com / eggledger123)
- **EggLedger API** on port `8080`
- **EggLedger Client** on port `5173`

### Option 3: Manual Setup

If you prefer to run components separately:

#### Database Setup

```bash
# Using Docker
docker run --name eggledger-postgres \
  -e POSTGRES_USER=eggledger \
  -e POSTGRES_PASSWORD=eggledger123 \
  -e POSTGRES_DB=eggledgerDB \
  -p 5432:5432 -d postgres:15-alpine
```

#### Backend Setup

```bash
cd EggLedger.API
dotnet restore
dotnet ef database update
dotnet run
```

API available at `http://localhost:8080`

#### Frontend Setup

```bash
cd EggLedger.Client
npm install
npm run dev
```

Frontend available at `http://localhost:5173`

## Configuration

### Backend Configuration

Copy `EggLedger.API/appsettings-example.json` to `EggLedger.API/appsettings.json` and configure:

```json
{
  "Jwt": {
    "SecretKey": "your-long-secure-jwt-secret-key-here",
    "Issuer": "EggLedgerAPI",
    "ExpiryInMinutes": 15,
    "Audience": "EggLedgerAudience"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=eggledger-postgres;Port=5432;Username=eggledger;Password=eggledger123;Database=eggledgerDB;"
  },
  "Authentication": {
    "Google": {
      "ClientSecret": "your-google-client-secret",
      "ClientId": "your-google-client-id"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "https://localhost:5173"],
    "PolicyName": "_myAllowSpecificOrigins"
  },
  "Environment": {
    "EGGLEDGER_LOG_PATH": {
      "Windows": "C:\\Logs\\EggLedger",
      "Linux": "/var/log/eggledger",
      "macOS": "/var/log/eggledger"
    }
  },
  "Ef_Migrate": "true"
}
```

### Frontend Environment Variables

The frontend is configured to connect to the API at `http://localhost:8080` by default. This can be customized in the Docker Compose environment variables:

```yaml
environment:
  - VITE_API_URL=http://localhost:8080
```

### Database Management

When using Docker Compose, pgAdmin is available at `http://localhost:5050`:
- **Email:** admin@eggledger.com
- **Password:** eggledger123

### Logging

EggLedger uses log4net for comprehensive logging. Log directories are automatically created by the application based on your operating system.

**Log Locations:**
- **Windows**: `C:\Logs\EggLedger\`
- **Linux**: `/var/log/eggledger/`
- **macOS**: `/var/log/eggledger/`

**Log Files:**
- `eggledger-api.log` - All application logs (INFO and above)
- `eggledger-api-errors.log` - Error logs only (ERROR and FATAL)

## API Endpoints

### Authentication
- `POST /egg-ledger-api/auth/login` - User login
- `POST /egg-ledger-api/auth/register` - User registration
- `GET /egg-ledger-api/auth/google-login` - Google OAuth

### Room Management
- `POST /egg-ledger-api/room/create` - Create room
- `POST /egg-ledger-api/room/join` - Join room
- `GET /egg-ledger-api/room/user/all` - Get user rooms

### Container Management
- `GET /egg-ledger-api/room/{roomCode}/container/all` - List containers
- `POST /egg-ledger-api/room/{roomCode}/container/create` - Create container
- `PUT /egg-ledger-api/room/{roomCode}/container/update/{id}` - Update container

### Order Management
- `POST /egg-ledger-api/{roomCode}/orders/stock` - Record purchase
- `POST /egg-ledger-api/{roomCode}/orders/consume` - Record consumption

## Development

### Aspire Dashboard

When running with .NET Aspire, you get access to a comprehensive dashboard at `https://localhost:17071` that provides:

- **Resource Overview**: Monitor all services (API, Frontend, Database)
- **Logs**: Centralized logging from all components
- **Metrics**: Performance metrics and telemetry
- **Traces**: Distributed tracing across services
- **Environment Variables**: Configuration management

### Development vs Production

The application is designed to work optimally in both development and production:

**Development (with Aspire):**
- Automatic PostgreSQL container management
- Service discovery and orchestration
- Unified dashboard and monitoring
- Hot reload for both frontend and backend

**Production (Docker Compose):**
- Production-optimized container configurations
- Resource limits and health checks
- pgAdmin for database management
- Environment-based configuration
- Container-ready with proper Dockerfile configurations

**Development (Docker Compose):**
- Same containerized setup as production
- Useful for testing production-like environments
- Consistent deployment across different machines
- Easy to share and reproduce development environments

### Building for Production

```bash
# Backend
cd EggLedger.API
dotnet publish -c Release

# Frontend
cd EggLedger.Client
npm run build
```

### Running Tests

```bash
dotnet test
```

## Architecture

```
EggLedger/
├── EggLedger.API/          # ASP.NET Core Web API
├── EggLedger.Models/       # Domain Models
├── EggLedger.Data/         # Entity Framework Context
├── EggLedger.Services/     # Business Logic Services
├── EggLedger.DTO/          # Data Transfer Objects
├── EggLedger.Client/       # Vue.js Frontend
├── EggLedger.AppHost/      # .NET Aspire Orchestration
├── EggLedger.ServiceDefaults/ # Aspire Service Defaults
└── EggLedger.sln          # Visual Studio Solution
```

The solution is organized as a .NET solution with integrated frontend orchestration:
- **Visual Studio 2022**: Open `EggLedger.sln` for backend development
- **Visual Studio Code**: Open the workspace for full-stack development
- **JetBrains Rider**: Native support for .NET solutions
- **.NET Aspire**: Handles frontend orchestration automatically

## Docker Services

When using Docker Compose, the following services are available:

| Service          | Port | Description                   |
|------------------|------|-------------------------------|
| PostgreSQL       | 5432 | Main database                 |
| pgAdmin          | 5050 | Database management interface |
| EggLedger API    | 8080 | Backend API                   |
| EggLedger Client | 5173 | Frontend application          |

## Documentation

API documentation available at `http://localhost:8080/scalar/v2` when running in development mode.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.