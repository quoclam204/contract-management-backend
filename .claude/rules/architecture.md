# Architecture Rules

## Clean Architecture Compliance

### Layer Dependencies (ENFORCED)

- **API Layer** → Depends on: Application, Infrastructure
- **Application Layer** → Depends on: Domain ONLY
- **Domain Layer** → Depends on: NOTHING (zero outward dependencies)
- **Infrastructure Layer** → Depends on: Application, Domain

### Dependency Direction (MUST FOLLOW)

```
API Layer
    ↓
Application Layer
    ↓
Domain Layer
Infrastructure Layer ↔ (Application & Domain)
```

### Layer Responsibilities

**Domain Layer:**

- ✅ ALLOWED: Entities, value objects, enums, domain events, domain services, interfaces
- ❌ PROHIBITED: Direct references to Application, Infrastructure, or API layers
- ❌ PROHIBITED: EF Core, database access, HTTP contexts, file system, external services
- ❌ PROHIBITED: DTOs, validation attributes, controller-specific attributes

**Application Layer:**

- ✅ ALLOWED: Use cases, DTOs, interfaces, application events, validators, mapping profiles
- ❌ PROHIBITED: Direct references to Infrastructure or API layers (except interfaces)
- ❌ PROHIBITED: EF Core DbContext, database connections, HTTP context
- ❌ PROHIBITED: Controller-specific code, model binding, action results

**Infrastructure Layer:**

- ✅ ALLOWED: EF Core configurations, DbContext, external service implementations, configuration providers
- ❌ PROHIBITED: Business logic that belongs in Domain or Application layers
- ❌ PROHIBITED: Controller-specific code, HTTP context references

**API Layer:**

- ✅ ALLOWED: Controllers, middleware, filters, model binding, action results, HTTP status codes
- ❌ PROHIBITED: Business logic that belongs in Domain or Application layers
- ❌ PROHIBITED: Direct database access, EF Core queries in controllers
- ❌ PROHIBITED: Complex validation logic (use Application layer validators)

### Module Boundary Rules (Modular Monolith)

1. **Prefer Isolation**: Modules should operate independently when possible
2. **Interface-Based**: Depend on interfaces, not concrete implementations from other modules
3. **Event-Driven**: Use events (MediatR) for loose coupling between modules
4. **No Circular Dependencies**: Module A → Module B and Module B → Module A is prohibited
5. **Shared Kernel**: Keep to absolute minimum; only truly shared interfaces/types
6. **Upstream Dependencies**: Dependencies should flow from more stable to less stable modules

### Contract Module Specific Rules

1. **Reference Implementation**: Follow Workflow module patterns exactly
2. **Database Fidelity**: Entities must match database.sql exactly
3. **Status Management**: Use enum values 0-7 for contract status
4. **Workflow Binding**: ContractTemplateVersion.WorkflowDefinitionId links to workflow
5. **Event Publishing**: Publish lifecycle events for integration
6. **Architectural Compliance**: Maintain Clean Architecture layer separation

## Violation Examples

```
# DO NOT DO THIS - Domain depending on Infrastructure
using ContractManagement.Infrastructure.Persistence;
public class ContractEntity { ... }

# DO NOT DO THIS - Domain with EF Core attributes
using System.ComponentModel.DataAnnotations.Schema;
public class Contract {
    [Column("ContractId")]
    public Guid Id { get; set; }
}

# DO NOT DO THIS - Application referencing Infrastructure
using ContractManagement.Infrastructure.Services;
public class ContractUseCase { ... }

# DO NOT DO THIS - API with business logic
[ApiController]
public class ContractController : ControllerBase {
    public IActionResult CalculateValue(Contract contract) {
        // Business logic belongs in Domain/Application, not API
        return contract.CalculateTotalValue();
    }
}
```

## Correct Patterns

```
// DO THIS - Clean Domain entity
public class Contract {
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = default!;
    // ... other properties

    public void Submit() {
        // Business logic in domain entity
        if (Status != ContractStatus.Draft)
            throw new InvalidOperationException("Only draft contracts can be submitted");

        Status = ContractStatus.PendingApproval;
        AddDomainEvent(new ContractSubmitted(this));
    }
}

// DO THIS - Application layer use case
public class SubmitContractHandler {
    public Task<Unit> Handle(SubmitContractCommand request, CancellationToken ct) {
        // Use case orchestration logic
        // ... validation, repository calls, event publishing
    }
}

// DO THIS - Infrastructure layer implementation
public class ContractConfiguration : IEntityTypeConfiguration<Contract> {
    public void Configure(EntityTypeBuilder<Contract> builder) {
        // EF Core configuration only
        builder.ToTable("CONTRACTS");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        // ... rest of configuration
    }
}

// DO THIS - Thin API controller
[ApiController]
[Route("api/[controller]")]
public class ContractController : ControllerBase {
    private readonly IMediator _mediator;

    public ContractController(IMediator mediator) {
        _mediator = mediator;
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id) {
        // Thin controller - delegates to Application layer
        var command = new SubmitContractCommand { Id = id };
        await _mediator.Send(command);
        return Ok();
    }
}
```

## Enforcement

These rules are enforced through:

- Code review (/review command)
- Architectural governance in pull requests
- Automated checks where possible
- Team awareness and adherence to principles
