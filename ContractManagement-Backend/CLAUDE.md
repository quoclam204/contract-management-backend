# Contract Management API - AI Development Rules

## Project

Contract Management System (CLM)

## Technology

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9
- SQL Server
- Clean Architecture
- Modular Monolith
- xUnit

## Architecture

The backend follows:

Controller
→ Application Service / Use Case
→ Domain
→ Infrastructure

## Layers

### Domain

Contains:
- Entities
- Value Objects
- Enums
- Domain Rules
- Domain Events

Domain must not depend on Infrastructure or ASP.NET Core.

### Application

Contains:
- Use Cases
- Application Services
- DTOs
- Interfaces / Abstractions

Application depends on Domain.

### Infrastructure

Contains:
- EF Core
- SQL Server
- Repository implementations
- RabbitMQ
- Hangfire
- Storage
- AI providers
- External integrations

Infrastructure depends on Application and Domain.

### API

Contains:
- Controllers
- Middleware
- Dependency Injection
- Authentication/Authorization configuration
- API configuration

Controllers must remain thin.

## Modular Monolith

Modules:

- Identity
- Contract
- Workflow
- Partner
- Payment
- Storage
- Notification
- AI

Do not place unrelated module logic in another module.

## Rules

- Do not invent requirements.
- Follow the SRS and approved specifications.
- Do not bypass Application to directly access Infrastructure.
- Do not put business logic in Controllers.
- Do not put EF Core dependencies in Domain.
- Do not create migrations unless explicitly requested by the Lead.
- Do not modify another developer's module without approval.
- Prefer small, focused changes.
- Add tests with implementation.
- Run build and tests after changes.

## Git

Use Conventional Commits.

Examples:

feat(contract): add contract creation use case
fix(workflow): prevent invalid approval transition
refactor(domain): extract contract status rules
test(contract): add state transition tests
docs(architecture): document module boundaries
chore(build): update dotnet dependencies

Do not use vague messages such as:

- update
- fix
- final
- done
- code
- changes