# Contract Management Constitution

## Principle 1 — Architecture First

All implementation must follow the approved Clean Architecture and Modular Monolith structure.

No feature may bypass the architectural boundaries.

## Principle 2 — Domain Independence

Domain must remain independent from frameworks and infrastructure.

## Principle 3 — Module Isolation

Each bounded context owns its business logic.

Modules communicate through explicit application contracts or domain/integration events.

## Principle 4 — Thin API

Controllers only handle HTTP concerns.

Business logic belongs to Application and Domain.

## Principle 5 — Testability

Every completed use case must have appropriate automated tests.

## Principle 6 — AI-Driven Development

AI agents must follow:

Design
→ Specification
→ Task Breakdown
→ Implementation
→ Testing
→ Human Review

AI must not invent requirements outside the approved specification.

## Principle 7 — Database Ownership

Only the Lead manages EF Core migrations and migration history.

Other developers may create entity classes but must not independently create migrations.

## Principle 8 — Git Discipline

Use Conventional Commits.

Every commit must represent one coherent change.

## Principle 9 — Code Ownership

Developers work inside their assigned module and must avoid modifying shared files unless coordinated.

## Principle 10 — Definition of Done

A task is not complete until:

- Code compiles
- Tests pass
- Architecture rules are respected
- No unrelated files are modified
- Specification is satisfied
- Human review is completed