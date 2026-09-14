# Partner Module Rules (Người 3)

## Scope of Work for Người 3
- **Partner CRUD**: Create, Read, Update, Delete operations for Partner entity.
- **IStorageProvider Abstraction**: Define and implement the storage provider interface for file operations.
- **Attachment Versioning**: Implement versioning mechanism for attachments (if applicable in Partner context).
- **Payment Tracking**: Track payments related to partners (if applicable).

## Architectural Guidelines
- Follow Clean Architecture principles as defined in `.claude/rules/architecture.md`.
- Adhere to coding standards in `.claude/rules/coding.md`.
- Ensure database fidelity to `database.sql` for any Partner-related tables.
- Use the Workflow module as reference for patterns where applicable.

## Domain Layer (Partner)
- Entity `Partner` must be a pure POCO with no outward dependencies.
- Properties: `Id` (Guid), `Name`, `TaxCode`, `Representative`, `ContactEmail`, `Address`, `CreatedAt`.
- No EF Core attributes or infrastructure concerns.

## Application Layer
- Define `IStorageProvider` interface in `Common/Interfaces` with methods:
  - `UploadFileAsync`
  - `DownloadFileAsync`
  - `GetPresignedUrlAsync`
  - `DeleteFileAsync`
- Partner DTOs and use cases in `Features/Partners/`.
- Use FluentValidation for request validation.

## Infrastructure Layer
- EF Core configuration for `Partner` entity mapping to `dbo.PARTNERS` table.
- Table name: `PARTNERS`
- Primary key: `Id`
- `TaxCode`: unique, max length 50
- `Name`: required, max length 300

## WebApi Layer
- RESTful controller for Partners under `/api/v1/partners`.
- Standard CRUD endpoints with appropriate HTTP status codes.
- Dependency injection for use cases and storage provider.

## Implementation Process
1. Always start with understanding requirements from SRS and tasks.
2. Follow the AI-driven development process: specification, breakdown, implementation, testing, review, commit.
3. Write unit tests for domain logic, application use cases, and infrastructure mappings.
4. Run `/test` command to verify build and tests.
5. Run `/review` command to check architectural compliance.
6. Use conventional commits and follow pull request process.

## Reminders
- Do not modify `database.sql` without explicit approval.
- Do not violate layer dependencies.
- Keep the database as the permanent source of truth.
- Use the Workflow module as a reference for patterns.