using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Dashboard.DTOs;
using ContractManagement.Application.Dashboard.Services;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using CurrentContract = ContractManagement.Domain.Contracts.Entities.Contract;
using CurrentContractType = ContractManagement.Domain.Contracts.Entities.ContractType;
using CurrentTemplateVersion = ContractManagement.Domain.Contracts.Entities.ContractTemplateVersion;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Domain;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContractManagement.UnitTests.Dashboard;

public class DashboardServiceTests : IDisposable
{
    private readonly TestDashboardDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDashboardDbContext(options);
        _service = new DashboardService(_context, _context, _context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static CurrentContract CreateContract(
        Guid ownerId,
        Guid partnerId,
        ContractStatus status,
        decimal value,
        DateTime createdAt,
        DateTime? expiryDate = null)
    {
        var effective = createdAt;
        var expiry = expiryDate ?? createdAt.AddYears(1);
        return new CurrentContract
        {
            Id = Guid.NewGuid(),
            ContractNumber = $"CTR-{Guid.NewGuid().ToString()[..8]}",
            Title = "Test Contract",
            OwnerId = ownerId,
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = partnerId,
            EffectiveDate = effective,
            ExpiryDate = expiry,
            Status = status,
            Value = value,
            CreatedAt = createdAt,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAccurateKPIs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, now, now.AddDays(10)), // expiringSoon
            CreateContract(ownerId, partnerId, ContractStatus.Active, 2000m, now, now.AddDays(60)), // active but not soon
            CreateContract(ownerId, partnerId, ContractStatus.Expiring, 500m, now, now.AddDays(5)), // status Expiring -> not counted
            CreateContract(ownerId, partnerId, ContractStatus.PendingApproval, 1500m, now),
            CreateContract(ownerId, partnerId, ContractStatus.Draft, 300m, now),
            CreateContract(ownerId, partnerId, ContractStatus.Signed, 700m, now),
            CreateContract(ownerId, partnerId, ContractStatus.Terminated, 400m, now)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(7, result.TotalContracts);
        Assert.Equal(6400m, result.TotalValue);
        Assert.Equal(2, result.ActiveContractsCount);
        Assert.Equal(3000m, result.ActiveContractsValue);
        Assert.Equal(1, result.ExpiringContractsCount);
        Assert.Equal(1000m, result.ExpiringContractsValue);
        Assert.Equal(1, result.PendingApprovalCount);
        Assert.Equal(1500m, result.PendingApprovalValue);
        Assert.Equal(1, result.DraftCount);
        Assert.Equal(1, result.SignedCount);
        Assert.Equal(1, result.TerminatedCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithEmptyDataset_ReturnsZeros()
    {
        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(0, result.TotalContracts);
        Assert.Equal(0m, result.TotalValue);
        Assert.Equal(0, result.ActiveContractsCount);
        Assert.Equal(0m, result.ActiveContractsValue);
        Assert.Equal(0, result.ExpiringContractsCount);
        Assert.Equal(0m, result.ExpiringContractsValue);
        Assert.Equal(0, result.PendingApprovalCount);
        Assert.Equal(0m, result.PendingApprovalValue);
    }

    [Fact]
    public async Task GetSummaryAsync_ExpiringSoon_OnlyActiveWithin30Days()
    {
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Active, 2000m, now, now.AddDays(30)),
            CreateContract(ownerId, partnerId, ContractStatus.Active, 3000m, now, now.AddDays(31)),
            CreateContract(ownerId, partnerId, ContractStatus.Active, 4000m, now, now.AddDays(-1)), // expired
            CreateContract(ownerId, partnerId, ContractStatus.Draft, 500m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Expiring, 600m, now, now.AddDays(5))
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetSummaryAsync();

        Assert.Equal(2, result.ExpiringContractsCount);
        Assert.Equal(3000m, result.ExpiringContractsValue); // 1000 + 2000
    }

    [Fact]
    public async Task GetSummaryAsync_ExpiringSoon_ExpiredNotCounted()
    {
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.Add(CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, now, now.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = await _service.GetSummaryAsync();

        Assert.Equal(0, result.ExpiringContractsCount);
        Assert.Equal(0m, result.ExpiringContractsValue);
    }

    [Fact]
    public async Task GetSummaryAsync_ExpiringSoon_Beyond30DaysNotCounted()
    {
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.Add(CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, now, now.AddDays(31)));
        await _context.SaveChangesAsync();

        var result = await _service.GetSummaryAsync();

        Assert.Equal(0, result.ExpiringContractsCount);
        Assert.Equal(0m, result.ExpiringContractsValue);
    }

    [Fact]
    public async Task GetSummaryAsync_ExpiringSoon_NonActiveNotCounted()
    {
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatus.Draft, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.PendingApproval, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Approved, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Signed, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Expiring, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Renewed, 1000m, now, now.AddDays(5)),
            CreateContract(ownerId, partnerId, ContractStatus.Terminated, 1000m, now, now.AddDays(5))
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetSummaryAsync();

        Assert.Equal(0, result.ExpiringContractsCount);
        Assert.Equal(0m, result.ExpiringContractsValue);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsAll8StatusesWithCorrectMetrics()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatus.Approved, 2500m, DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByStatusAsync();

        // Assert
        Assert.Equal(8, result.Count);

        var activeStatus = result.Single(s => s.Status == (byte)ContractStatus.Active);
        Assert.Equal("Active", activeStatus.StatusName);
        Assert.Equal(1, activeStatus.Count);
        Assert.Equal(1000m, activeStatus.TotalValue);

        var approvedStatus = result.Single(s => s.Status == (byte)ContractStatus.Approved);
        Assert.Equal("Approved", approvedStatus.StatusName);
        Assert.Equal(1, approvedStatus.Count);
        Assert.Equal(2500m, approvedStatus.TotalValue);

        var draftStatus = result.Single(s => s.Status == (byte)ContractStatus.Draft);
        Assert.Equal(0, draftStatus.Count);
        Assert.Equal(0m, draftStatus.TotalValue);
    }

    [Fact]
    public async Task GetByDepartmentAsync_GroupsByDepartmentCorrectly()
    {
        // Arrange
        var deptA = new Department { Id = Guid.NewGuid(), Name = "Legal" };
        var deptB = new Department { Id = Guid.NewGuid(), Name = "Sales" };
        _context.Departments.AddRange(deptA, deptB);

        var userA = new User("User A", "usera@test.com", "hash", UserRole.Staff, deptA.Id);
        var userB = new User("User B", "userb@test.com", "hash", UserRole.Staff, deptB.Id);
        var userUnassigned = new User("User C", "userc@test.com", "hash", UserRole.Staff, null);
        _context.Users.AddRange(userA, userB, userUnassigned);

        var partnerId = Guid.NewGuid();
        _context.Contracts.AddRange(
            CreateContract(userA.Id, partnerId, ContractStatus.Active, 5000m, DateTime.UtcNow),
            CreateContract(userA.Id, partnerId, ContractStatus.Signed, 3000m, DateTime.UtcNow),
            CreateContract(userB.Id, partnerId, ContractStatus.Active, 4000m, DateTime.UtcNow),
            CreateContract(userUnassigned.Id, partnerId, ContractStatus.Active, 1000m, DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByDepartmentAsync();

        // Assert
        Assert.Equal(3, result.Count);

        var legalGroup = result.Single(r => r.DepartmentId == deptA.Id);
        Assert.Equal("Legal", legalGroup.DepartmentName);
        Assert.Equal(2, legalGroup.Count);
        Assert.Equal(8000m, legalGroup.TotalValue);

        var salesGroup = result.Single(r => r.DepartmentId == deptB.Id);
        Assert.Equal("Sales", salesGroup.DepartmentName);
        Assert.Equal(1, salesGroup.Count);
        Assert.Equal(4000m, salesGroup.TotalValue);

        var unassignedGroup = result.Single(r => r.DepartmentId == null);
        Assert.Equal("Unassigned", unassignedGroup.DepartmentName);
        Assert.Equal(1, unassignedGroup.Count);
        Assert.Equal(1000m, unassignedGroup.TotalValue);
    }

    [Fact]
    public async Task GetByPartnerAsync_GroupsByPartnerAndAppliesTopLimit()
    {
        // Arrange
        var partnerA = new Partner(Guid.NewGuid(), "Partner Alpha", "111", "Rep A", "a@test.com", "Addr A", DateTime.UtcNow);
        var partnerB = new Partner(Guid.NewGuid(), "Partner Beta", "222", "Rep B", "b@test.com", "Addr B", DateTime.UtcNow);
        var partnerC = new Partner(Guid.NewGuid(), "Partner Gamma", "333", "Rep C", "c@test.com", "Addr C", DateTime.UtcNow);
        _context.Partners.AddRange(partnerA, partnerB, partnerC);

        var ownerId = Guid.NewGuid();
        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerA.Id, ContractStatus.Active, 10000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerB.Id, ContractStatus.Active, 5000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerC.Id, ContractStatus.Active, 1000m, DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Act: Top 2
        var result = await _service.GetByPartnerAsync(top: 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(partnerA.Id, result[0].PartnerId);
        Assert.Equal("Partner Alpha", result[0].PartnerName);
        Assert.Equal(10000m, result[0].TotalValue);

        Assert.Equal(partnerB.Id, result[1].PartnerId);
        Assert.Equal("Partner Beta", result[1].PartnerName);
        Assert.Equal(5000m, result[1].TotalValue);
    }

    [Fact]
    public async Task GetByTimeAsync_GroupsByYearAndMonth()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        var dateJan = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var dateFeb = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatus.Active, 1000m, dateJan),
            CreateContract(ownerId, partnerId, ContractStatus.Signed, 2000m, dateJan),
            CreateContract(ownerId, partnerId, ContractStatus.Active, 3000m, dateFeb)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByTimeAsync();

        // Assert
        Assert.Equal(2, result.Count);

        var janGroup = result.Single(r => r.Year == 2026 && r.Month == 1);
        Assert.Equal(2, janGroup.Count);
        Assert.Equal(3000m, janGroup.TotalValue);

        var febGroup = result.Single(r => r.Year == 2026 && r.Month == 2);
        Assert.Equal(1, febGroup.Count);
        Assert.Equal(3000m, febGroup.TotalValue);
    }

    [Fact]
    public async Task GetSummaryAsync_WithUserIdFilter_ReturnsOnlyUserContracts()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(userA, partnerId, ContractStatus.Active, 1000m, DateTime.UtcNow),
            CreateContract(userA, partnerId, ContractStatus.Active, 2000m, DateTime.UtcNow),
            CreateContract(userB, partnerId, ContractStatus.Active, 5000m, DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Act: Filter by User A
        var resultA = await _service.GetSummaryAsync(userIdFilter: userA);

        // Assert
        Assert.Equal(2, resultA.TotalContracts);
        Assert.Equal(3000m, resultA.TotalValue);
        Assert.Equal(2, resultA.ActiveContractsCount);
    }
}

public class TestDashboardDbContext : DbContext, IContractDbContext, IIdentityDbContext, IPartnerDbContext
{
    public TestDashboardDbContext(DbContextOptions<TestDashboardDbContext> options) : base(options) { }

    public DbSet<CurrentContract> Contracts { get; set; } = default!;
    public DbSet<CurrentContractType> ContractTypes { get; set; } = default!;
    public DbSet<CurrentTemplateVersion> ContractTemplateVersions { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Department> Departments { get; set; } = default!;
    public DbSet<Partner> Partners { get; set; } = default!;

    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    public DbSet<WorkflowStep> WorkflowSteps { get; set; } = default!;
    public DbSet<ApprovalStep> ApprovalSteps { get; set; } = default!;
    public DbSet<Signature> Signatures { get; set; } = default!;

    public Task<Guid> GetDefaultApproverIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.NewGuid());
    }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CurrentContract>().ToTable("CONTRACTS");
        modelBuilder.Entity<CurrentContractType>().ToTable("CONTRACT_TYPES");
        modelBuilder.Entity<CurrentTemplateVersion>().ToTable("CONTRACT_TEMPLATE_VERSIONS");
        modelBuilder.Entity<User>().ToTable("USERS");
        modelBuilder.Entity<Department>().ToTable("DEPARTMENTS");
        modelBuilder.Entity<Partner>().ToTable("PARTNERS");
    }
}
