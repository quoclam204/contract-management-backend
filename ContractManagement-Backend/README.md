# Contract Management API

This repository contains the backend for the Contract Management System built with .NET 9 following Clean Architecture and Modular Monolith principles.

## Architecture

The solution follows:

```
Controller
    ↓
Application Service / Use Case
    ↓
Domain
    ↓
Infrastructure
```

### Layers

- **Domain**: Contains entities, value objects, enums, domain rules, and domain events. Has no dependencies on other layers or external frameworks.
- **Application**: Contains use cases, application services, DTOs, and interfaces. Depends only on Domain.
- **Infrastructure**: Contains EF Core persistence, SQL Server configuration, repository implementations, and external service integrations. Depends on Application and Domain.
- **API**: Contains controllers, middleware, and dependency injection configuration. Depends on Application and Infrastructure for composition root.

### Modular Monolith

The system is organized into the following bounded contexts:

1. Identity
2. Contract
3. Workflow
4. Partner
5. Payment
6. Storage / Attachment
7. Notification
8. AI

Each context is represented in all layers (Domain, Application, Infrastructure) to maintain module boundaries.

## Technology Stack

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core 9.0
- SQL Server
- Clean Architecture
- Modular Monolith
- xUnit for testing
- Serilog for logging
- Swashbuckle for API documentation

## Getting Started

1. Ensure you have [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed.
2. Clone the repository.
3. Navigate to the solution directory.
4. Run `dotnet build` to build the solution.
5. Run `dotnet test` to execute all tests.
6. Run `dotnet run --project src/ContractManagement.Api` to start the API.

## AI-Driven Development

This repository follows AI-driven development principles:

- See `CLAUDE.md` for development rules and guidelines.
- See `.specify/memory/constitution.md` for architectural principles.
- Specifications are located in the `specs/` directory.
- All implementation must follow the approved architecture and specifications.

## Conventional Commits

Please use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages:

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation changes
- `style`: Formatting, missing semi-colons, etc.
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or correcting tests
- `chore`: Changes to the build process or auxiliary tools

Examples:
- `feat(contract): add contract creation use case`
- `fix(workflow): prevent invalid approval transition`
- `refactor(domain): extract contract status rules`
- `test(contract): add state transition tests`
- `docs(architecture): document module boundaries`
- `chore(build): update dotnet dependencies`

## License

This project is licensed under the MIT License.