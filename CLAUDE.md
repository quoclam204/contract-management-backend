# ContractManagement Project - Claude Code Configuration

This document outlines the Claude Code setup for the ContractManagement project, providing AI-assisted development capabilities while strictly adhering to the project's architectural principles and constraints.

## Project Overview

The ContractManagement project is a Modular Monolith implementation following Clean Architecture principles with .NET 10.0 target framework. It consists of 8 bounded contexts:

1. Identity - User authentication, roles, permissions
2. Contract - Contract lifecycle, types, templates (primary focus)
3. Workflow - Process automation, approvals (reference implementation)
4. Partner - Counterparty management
5. Payment - Financial transactions, invoicing
6. Storage/Attachment - File storage, document management
7. Notification - Alerting, messaging delivery
8. AI - Contract analysis, insights

## 🚫 Critical Constraints

**DO NOT MODIFY:**

- Application source code (outside of Claude-approved feature work)
- Database schema (database.sql is PERMANENT source of truth)
- Git history

**CLAUDE CODE USAGE IS LIMITED TO:**

- Creating/updating Claude Code configuration files
- Documentation, rules, skills, agents, commands, playbooks
- Memory files and related configuration
- AI-assisted development following established processes

## 📁 .claude Directory Structure

```
.claude/
├── agents/                 # Specialized AI agents
│   ├── architecture-reviewer.md
│   ├── contract-reviewer.md
│   └── database-reviewer.md
├── commands/               # Slash commands for workflows
│   ├── inspect.md          # Initial inspection command
│   ├── review.md           # Architecture & code review
│   ├── test.md             # Build and test execution
│   └── contract.md         # Contract-module specific workflow
├── memory/                 # Persistent knowledge base
│   ├── architecture.md     # Architecture principles & patterns
│   ├── contract-module.md  # Contract module specifics
│   ├── database.md         # Database schema & rules (PERMANENT)
│   └── development-notes.md # Development process & standards
├── playbooks/              # Step-by-step workflows
│   ├── implement-feature.md # Feature implementation process
│   ├── contract-feature.md  # Contract-specific feature workflow
│   ├── bug-fix.md          # Bug diagnosis and fix process
│   └── pull-request.md     # PR preparation and submission
├── rules/                  # Coding and architectural guidelines
│   ├── architecture.md     # Clean Architecture & Modular Monolith
│   ├── coding.md           # Coding standards & conventions
│   ├── database.md         # Database access & EF Core rules
│   ├── testing.md          # Testing strategies & practices
│   └── git.md              # Git workflow & conventions
├── skills/                 # Reusable skill definitions
│   ├── code-review/        # Code review skill
│   │   └── SKILL.md
│   └── contract-management/ # Contract management skill
│       └── SKILL.md
├── settings.json           # Claude Code permission settings
├── settings.example.json   # Example settings template
└── scripts/                # Utility scripts
    └── README.md
```

## 🔧 Key Commands

### `/contract`

**MANDATORY first step for ANY Contract module work**

- Inspects requirements from 02_Task_Nguoi2_Contract.md
- Reviews database.sql for Contract tables
- Examines Workflow module as reference implementation
- Checks existing Contract code
- Proposes implementation plan
- **WAIT FOR EXPLICIT APPROVAL before coding**

### `/implement-feature`

Starts feature implementation process:

1. Understand requirements
2. Run Contract command (for Contract features)
3. Break down work
4. Implement following patterns
5. Test thoroughly
6. Review process
7. Prepare for commit
8. Create pull request

### `/test`

Runs appropriate build and tests:

- `dotnet build` to ensure solution compiles
- Executes relevant unit tests based on changes
- Runs integration tests for database changes
- Reports failures with clear pass/fail status

### `/review`

Checks for compliance with:

- Architectural principles (Clean Architecture, Modular Monolith)
- Coding standards and conventions
- Database schema fidelity (match to database.sql)
- Contract module specific requirements
- Test coverage and quality

### `/pull-request`

Prepares clean PR for review:

1. Ensure work is complete
2. Local validation (build and test)
3. Inspect changes (git diff)
4. Commit discipline (conventional commits)
5. Final review (quality check)
6. Prepare PR description
7. Request review
8. Post-merge cleanup

## 🏗️ Architecture Compliance

### Clean Architecture Layers (DO NOT VIOLATE)

```
API Layer → Application Layer → Domain Layer
                    ↑
          Infrastructure Layer ↔ (Application & Domain)
```

**Domain Layer**: Pure business logic, ZERO outward dependencies
**Application Layer**: Use cases, DTOs, interfaces (depends ONLY on Domain)
**Infrastructure Layer**: EF Core, external services (depends on Application & Domain)
**API Layer**: Controllers, middleware (depends on Application & Infrastructure)

### Modular Monolith Boundaries

The project maintains 8 strictly bounded contexts with:

- Interface-based communication between modules
- Event-driven integration (MediatR pattern)
- Preference for module isolation
- Absolutely no circular dependencies
- Shared kernel kept to minimum

## 📚 Contract Module Reference

The **Workflow module is the PERMANENT reference implementation** for Contract module development. Follow these patterns exactly:

### Domain Layer

- Pure POCO entities (Contract, ContractType, ContractTemplateVersion)
- Enums for status values (0-7)
- Value objects (ContractNumber, ContractValue)
- Domain events for lifecycle transitions
- Business logic in entities and domain services

### Application Layer

- DTOs for data transfer
- Interfaces for services and repositories
- MediatR handlers for use cases
- FluentValidation for validation
- AutoMapper for mapping
- Application events for integration

### Infrastructure Layer

- EF Core entity configurations
- DbContext with proper DbSets
- Repository implementations
- External service wrappers

### API Layer

- Thin controllers (minimal business logic)
- RESTful endpoints
- Proper HTTP status codes
- Dependency injection

## 💾 Database Rules (PERMANENT)

**database.sql is the UNCHANGEABLE source of truth** for database schema:

1. **NO SCHEMA INVENTION** - Do not invent columns, tables, or relationships
2. **EXACT MATCHING** - EF Core configurations must match database.sql exactly
3. **PRESERVE CONSTRAINTS** - Keep all PKs, FKs, unique constraints, check constraints
4. **PRESERVE INDEXES** - Keep all existing indexes for performance
5. **RESPECT CONCURRENCY** - RowVersion columns MUST be preserved and used
6. **NO AUTO MIGRATIONS** - Do not create or apply migrations automatically
7. **STATUS VALUES** - CONTRACTS.Status values 0-7 are PERMANENT:
   - 0 = Draft
   - 1 = PendingApproval
   - 2 = Approved
   - 3 = Signed
   - 4 = Active
   - 5 = Expiring
   - 6 = Renewed
   - 7 = Terminated
8. **FK INTEGRITY** - All foreign key relationships must be maintained
9. **NO DESTRUCTIVE OPERATIONS** - Never drop tables or lose data automatically

### Permanent Contract Tables

- **CONTRACT_TYPES** - Id, Name, CreatedAt
- **CONTRACT_TEMPLATE_VERSIONS** - Id, ContractTypeId, Version, TemplateFileUrl, ContentJson, WorkflowDefinitionId, IsActive, CreatedBy, CreatedAt
- **CONTRACTS** - Id, ContractNumber, ContractTypeId, TemplateVersionUsedId, PartnerId, OwnerId, Title, Value, SignedDate, EffectiveDate, ExpiryDate, Status, FileUrl, ParentContractId, CreatedAt, UpdatedAt, RowVersion

## 🧪 Testing Strategy

### Unit Tests

- Domain entities (validation, business logic)
- Domain services (business rules)
- Application handlers (use case logic)
- Application validators (validation rules)
- Mapping profiles (DTO/entity conversion)
- Follow Arrange-Act-Assert pattern
- Test happy path, edge cases, and error conditions

### Integration Tests

- EF Core configurations (database mapping)
- Repository methods (if implemented)
- Controller endpoints (API behavior)
- Mock external dependencies appropriately

### Test Organization

- `tests/ContractManagement.UnitTests/`
- `tests/ContractManagement.IntegrationTests/`
- Module-specific subfolders when they exist (Contracts/, Workflow/)

## 📝 Coding Standards

## 📌 Git Commit Convention

This project strictly adheres to the **Conventional Commits** specification for all commit messages across both the **.NET API backend** and **React frontend**.

### 1. Commit Message Format

The standard commit message format is:

```text
<type>: <description>
```

Or optionally with an explicit scope:

```text
<type>(<scope>): <description>
```

#### Allowed Scopes (Optional):

- **Backend (.NET)**: `contract`, `workflow`, `identity`, `partner`, `payment`, `storage`, `notification`, `ai`, `api`, `infrastructure`, `domain`, `db`
- **Frontend (React)**: `auth`, `contract`, `workflow`, `dashboard`, `components`, `hooks`, `ui`

### 2. Allowed Commit Types

| Type       | Purpose                     | When to Use                                                          |
| :--------- | :-------------------------- | :------------------------------------------------------------------- |
| `feat`     | Add a new feature           | Introducing a new endpoint, entity, use case, or UI component.       |
| `fix`      | Fix a bug                   | Patching a bug, error, broken validation, or logic defect.           |
| `refactor` | Restructure or improve code | Code refactoring that neither adds a feature nor fixes a bug.        |
| `docs`     | Documentation changes       | Updating SRS, markdown docs, API specs, diagrams, or comments.       |
| `test`     | Add or modify tests         | Adding or updating unit tests, integration tests, or test fixtures.  |
| `chore`    | Configuration & maintenance | Updating packages, build scripts, Docker setup, or repo maintenance. |
| `style`    | Code formatting & style     | Formatting, linting, whitespace, or naming without logic changes.    |

### 3. Commit Message Rules & Quality Standards

- **Imperative Mood**: Write in the imperative mood (e.g., `add`, `implement`, `fix`, `refactor` — NOT `added`, `fixing`, `fixes`).
- **Language**: All commit messages must be written in **English**.
- **Short & Concise**: Keep the subject line short, clear, and meaningful (aim for ≤ 72 characters).
- **Descriptive**: Accurately describe what changed and why in the codebase.
- **Lowercase**: Use lowercase for type and starting character of description (e.g., `feat: implement ...`).

### 4. 🚫 Strictly Forbidden Vague Messages

Do NOT write vague, lazy, or ambiguous commit messages, such as:

- ❌ `update code`
- ❌ `fix`
- ❌ `changes`
- ❌ `done`
- ❌ `update`
- ❌ `final`
- ❌ `modified files`
- ❌ `fix bug`
- ❌ `test`

### 5. 🤖 Rules for AI Assistants

- Whenever an AI assistant creates, suggests, or executes a Git commit, it **MUST** read and strictly follow the Git Commit Convention defined in this `CLAUDE.md`.
- AI must inspect the staged changes (`git diff --staged`) to formulate a precise `<type>: <description>` or `<type>(<scope>): <description>`.
- AI must NEVER use or suggest any of the forbidden vague commit messages listed above.

### 6. Practical Project Examples

#### Backend (.NET API):

- `feat(contract): implement contract draft creation use case`
- `feat(workflow): add multi-step approval routing engine`
- `feat(storage): implement MinIO storage provider for attachments`
- `fix(workflow): resolve null reference exception in condition evaluator`
- `fix(auth): correct token expiration calculation in JWT handler`
- `refactor(persistence): modularize entity configurations per bounded context`
- `refactor(api): clean up Program.cs with extension methods for layer DI`
- `docs: update SRS v2 architecture and sequence diagrams`
- `test(workflow): add unit tests for approval step transitions`
- `chore: add Serilog and Seq structured logging configuration`
- `style: format C# entity classes according to naming conventions`

#### Frontend (React):

- `feat(contract): add contract creation form with template selector`
- `feat(dashboard): integrate Recharts for contract status analytics`
- `fix(auth): handle refresh token race condition on 401 response`
- `refactor(components): extract reusable Modal dialog with Shadcn UI`
- `test(hooks): add unit tests for useContractApproval custom hook`
- `chore: update TanStack Query to version 5.x`

### Code Style

- Follow existing code style and conventions
- Use meaningful names for variables, methods, classes
- Keep methods small and focused (single responsibility)
- Write self-documenting code with clear intent
- Add XML documentation for public APIs
- Handle errors appropriately (don't swallow exceptions)
- Avoid hard-coded values (use configuration/constants)
- Follow .NET 10 and C# 12 best practices

## 🔄 AI-Driven Development Process

This is the PERMANENT process that will NEVER change:

1. **Specification First**: Start with clear requirements from docs/tasks/\*.md
2. **Task Breakdown**: Use `/implement-feature` to break down into specific tasks
3. **Implementation**: Code following established patterns (Workflow module as reference)
4. **Testing**: Write unit tests, run with `/test` command
5. **Review**: Use `/review` command to check for architectural compliance
6. **Commit**: Use conventional commits, then `/pull-request` playbook
7. **Repeat**: Next feature

### Contract Module Specific Process

1. **ALWAYS start with `/contract` command** (mandatory)
2. Wait for explicit approval before any coding
3. Follow Workflow module patterns exactly
4. Ensure database fidelity to database.sql
5. Maintain architectural layer separation
6. Write comprehensive tests
7. Review before committing
8. Use conventional commits and PR playbook

## ⚠️ Important Reminders

- **The Contract command is your first step for ANY Contract work** - never skip it
- **database.sql is PERMANENT** - this is your anchor for all database work
- **Workflow module is the PERMANENT reference** - copy it faithfully for Contract module
- **AI-Driven Development requires following the complete process** - don't skip steps
- **Review is mandatory** - it protects architectural integrity and code quality
- **Small, focused commits** are easier to review and safer than large batch commits
- **Testing is your safety net** - write tests before and after fixing/implementing
- **Document assumptions** and non-obvious fixes for future maintainers
- **Never modify application source code, database schema, or Git history** outside of approved feature work
- **Only create/update Claude Code configuration, documentation, rules, skills, agents, commands, playbooks, memory, and related configuration files**

## 📋 Getting Started

1. Familiarize yourself with the .claude directory structure
2. For any Contract module work, ALWAYS start with: `/contract`
3. Wait for explicit approval before proceeding
4. Follow the processes outlined in the playbooks
5. Maintain strict adherence to architectural principles
6. Keep the database.sql as your permanent source of truth
7. Use the Workflow module as your reference implementation

This configuration enables AI-assisted development while protecting the architectural integrity and contractual obligations of the ContractManagement project.
