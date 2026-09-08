# Contributing to EggLedger

Thank you for your interest in contributing to EggLedger! Whether you are reporting a bug, improving documentation, or proposing new features, your help is welcome.


## Code of Conduct

We are committed to providing a friendly, welcoming, and inclusive environment for everyone. Please be respectful and constructive in all discussions and pull requests.


## How to Contribute

### Reporting Bugs & Requesting Features

- Search the existing [GitHub Issues](https://github.com/sheshankchandra/EggLedger/issues) to see if your bug or feature request has already been reported.
- If not, open a new issue with a clear description, reproduction steps (for bugs), or expected behavior and use cases (for features).

### Developing & Submitting Changes

1. **Fork and clone** the repository:
   ```bash
   git clone https://github.com/<your-username>/EggLedger.git
   cd EggLedger
   ```
2. **Create a branch** for your work:
   ```bash
   git checkout -b feat/your-feature-name
   ```
3. Follow the project's **architectural layering** and **coding standards** described below.
4. **Run tests** and verify builds pass before opening a PR.
5. Submit a **Pull Request** targeting the `master` branch with a concise summary of changes and rationale.


## Architecture & Layering Rules

EggLedger enforces strict separation of concerns across its solution layers:

- **EggLedger.API**: Thin controllers, middleware, dependency injection, and configuration extensions.
  - Controllers validate requests and delegate immediately.
  - **No business logic in controllers**.
  - All endpoints return DTOs; never return EF Core entities directly.
- **EggLedger.Services**: Encapsulates all domain and business logic.
  - Methods return `FluentResults.Result<T>` for predictable, typed error handling instead of throwing exceptions for normal failure cases.
- **EggLedger.Data**: `DbContext`, entity configurations, and EF Core migrations.
- **EggLedger.Models**: Database-mapped entity definitions.
- **EggLedger.DTO**: Request/response contracts.
- **EggLedger.ServiceDefaults**: Shared Aspire defaults (health checks, OpenTelemetry).
- **EggLedger.AppHost**: Local development orchestration via .NET Aspire (development only).
- **EggLedger.Client**: Vue 3 SPA frontend.
- **EggLedger.Tests**: xUnit integration test suite using Testcontainers.


## Coding Standards

### Backend (.NET / C#)

- **C# 13 / .NET 10**: Use modern C# language features (file-scoped namespaces, pattern matching, primary constructors where appropriate).
- **Central Package Management (CPM)**: All package versions are pinned in `Directory.Packages.props`. Solution projects reference packages without specifying a `Version` attribute.
- **Async Throughout**: Suffix asynchronous methods with `Async` and pass `CancellationToken` when appropriate.
- **Logging**: Use injected `ILogger<T>`. Never use `Console.WriteLine`.
- **Formatting**: Adhere to `.editorconfig`. Run `dotnet format` prior to committing.

### Frontend (Vue 3 / JavaScript)

- **Composition API**: Use `<script setup>` syntax for all components.
- **State Management**: Centralize shared state in Pinia stores (`src/stores/`).
- **Styling**: Use design tokens and CSS custom properties; ensure all views support both Light and Dark themes.
- **Formatting & Linting**:
  ```bash
  cd EggLedger.Client
  npm run lint
  npm run format
  ```


## Running Tests

Integration tests run against a real PostgreSQL instance managed automatically via [Testcontainers](https://testcontainers.com/):

```bash
# Ensure Docker Desktop is running
dotnet test
```

Continuous Integration (CI) automatically validates builds, lints, and test execution for all pull requests.


## License

By contributing to EggLedger, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).
