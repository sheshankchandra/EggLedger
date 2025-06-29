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
**Documentation:** OpenAPI/Swagger

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- PostgreSQL 13+

### Database Setup

```bash
# Using Docker
docker run --name eggledger-postgres \
  -e POSTGRES_USER=asapdb \
  -e POSTGRES_PASSWORD=asap \
  -e POSTGRES_DB=EggLedgerDB \
  -p 5432:5432 -d postgres:13
```

### Backend Setup

```bash
cd EggLedger.API
dotnet restore
dotnet ef database update
dotnet run
```

API available at `https://localhost:7224`

### Frontend Setup

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
├── EggLedger.API/          # Web API
├── EggLedger.Core/         # Business Logic
├── EggLedger.Client/       # Vue.js Frontend
└── EggLedger.AppHost/      # Orchestration
```

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