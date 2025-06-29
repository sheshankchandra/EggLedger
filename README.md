# EggLedger

A full-stack roommate resource and expense management application built with ASP.NET Core and Vue.js. Track shared groceries, supplies, and household expenses with room-based organization and automatic balance calculations.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Vue.js](https://img.shields.io/badge/Vue.js-3.x-green.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-13+-blue.svg)

## Features

- **Room-based organization** with unique room codes
- **Resource tracking** for shared items and expenses
- **Automatic balance calculations** using FIFO order tracking
- **User authentication** with JWT and Google OAuth
- **Real-time updates** for stock levels and consumption
- **Role-based permissions** for room admins and members

## Technology Stack

**Backend:** ASP.NET Core 9.0, Entity Framework Core, PostgreSQL  
**Frontend:** Vue.js 3, Vite, Pinia  
**Authentication:** JWT, Google OAuth 2.0  
**Orchestration:** .NET Aspire 9.3 (development), Docker (production)  
**Monitoring:** Built-in health checks, OpenTelemetry  
**Documentation:** OpenAPI/Swagger

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

### Option 1: Using .NET Aspire (Recommended)

.NET Aspire provides orchestration for the entire application stack including PostgreSQL, API, and frontend.

```bash
# Clone the repository
git clone https://github.com/yourusername/eggledger.git
cd eggledger

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
- Start PostgreSQL in a Docker container
- Run the API server with proper database connection
- Start the Vue.js development server
- Provide a unified dashboard at `https://localhost:17071`

### Option 2: Manual Setup

If you prefer to run components separately:

#### Database Setup

```bash
# Using Docker
docker run --name eggledger-postgres \
  -e POSTGRES_USER=asapdb \
  -e POSTGRES_PASSWORD=asap \
  -e POSTGRES_DB=EggLedgerDB \
  -p 5432:5432 -d postgres:13
```

#### Backend Setup

```bash
cd EggLedger.API
dotnet restore
dotnet ef database update
dotnet run
```

API available at `https://localhost:7224`

#### Frontend Setup

```bash
cd EggLedger.Client
npm install
npm run dev
```

Frontend available at `http://localhost:5173`

## Configuration

### Backend Environment Variables

```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Username=asapdb;Password=asap;Database=EggLedgerDB
Jwt__SecretKey=your-secret-key
Jwt__Issuer=EggLedger
Jwt__Audience=EggLedger
```

### Frontend Environment Variables

```
VITE_API_BASE_URL=https://localhost:7224
```

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

**Production (traditional deployment):**
- Direct database connections with retry policies
- Health check endpoints for load balancers and orchestrators
- Environment-based configuration
- Container-ready with proper Dockerfile configurations

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
├── EggLedger.Core/         # Shared Business Logic
├── EggLedger.Client/       # Vue.js Frontend
├── EggLedger.AppHost/      # .NET Aspire Orchestration
├── EggLedger.ServiceDefaults/ # Aspire Service Defaults
└── EggLedger.sln          # Visual Studio Solution
```

The solution is organized as a .NET solution with integrated frontend orchestration:
- **Visual Studio 2022**: Open `EggLedger.sln` for backend development
- **Visual Studio Code**: Open `EggLedger.code-workspace` for full-stack development
- **JetBrains Rider**: Native support for .NET solutions
- **.NET Aspire**: Handles frontend orchestration automatically

## Documentation

API documentation available at `https://localhost:7224/scalar/v2` when running in development mode.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.