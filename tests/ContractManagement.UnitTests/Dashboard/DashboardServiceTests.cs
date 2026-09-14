using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Application.Dashboard.DTOs;
using ContractManagement.Application.Dashboard.Services;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using DomainContract = ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contract.Enums;
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
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static DomainContract.Contract CreateContract(
        Guid ownerId,
        Guid partnerId,
        ContractStatusEnum status,
        decimal value,
        DateTime createdAt)
    {
        return new DomainContract.Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = $"CTR-{Guid.NewGuid().ToString()[..8]}",
            Title = "Test Contract",
            OwnerId = ownerId,
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = partnerId,
            EffectiveDate = createdAt,
            ExpiryDate = createdAt.AddYears(1),
            Status = (ContractStatus)(byte)status,
            Value = value,
            CreatedAt = createdAt,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAccurateKPIs()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatusEnum.Active, 1000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Active, 2000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Expiring, 500m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.PendingApproval, 1500m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Draft, 300m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Signed, 700m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Terminated, 400m, DateTime.UtcNow)
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
        Assert.Equal(500m, result.ExpiringContractsValue);
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
    public async Task GetByStatusAsync_ReturnsAll8StatusesWithCorrectMetrics()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        _context.Contracts.AddRange(
            CreateContract(ownerId, partnerId, ContractStatusEnum.Active, 1000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Approved, 2500m, DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByStatusAsync();

        // Assert
        Assert.Equal(8, result.Count);

        var activeStatus = result.Single(s => s.Status == (byte)ContractStatusEnum.Active);
        Assert.Equal("Active", activeStatus.StatusName);
        Assert.Equal(1, activeStatus.Count);
        Assert.Equal(1000m, activeStatus.TotalValue);

        var approvedStatus = result.Single(s => s.Status == (byte)ContractStatusEnum.Approved);
        Assert.Equal("Approved", approvedStatus.StatusName);
        Assert.Equal(1, approvedStatus.Count);
        Assert.Equal(2500m, approvedStatus.TotalValue);

        var draftStatus = result.Single(s => s.Status == (byte)ContractStatusEnum.Draft);
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
            CreateContract(userA.Id, partnerId, ContractStatusEnum.Active, 5000m, DateTime.UtcNow),
            CreateContract(userA.Id, partnerId, ContractStatusEnum.Signed, 3000m, DateTime.UtcNow),
            CreateContract(userB.Id, partnerId, ContractStatusEnum.Active, 4000m, DateTime.UtcNow),
            CreateContract(userUnassigned.Id, partnerId, ContractStatusEnum.Active, 1000m, DateTime.UtcNow)
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
            CreateContract(ownerId, partnerA.Id, ContractStatusEnum.Active, 10000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerB.Id, ContractStatusEnum.Active, 5000m, DateTime.UtcNow),
            CreateContract(ownerId, partnerC.Id, ContractStatusEnum.Active, 1000m, DateTime.UtcNow)
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
            CreateContract(ownerId, partnerId, ContractStatusEnum.Active, 1000m, dateJan),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Signed, 2000m, dateJan),
            CreateContract(ownerId, partnerId, ContractStatusEnum.Active, 3000m, dateFeb)
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
            CreateContract(userA, partnerId, ContractStatusEnum.Active, 1000m, DateTime.UtcNow),
            CreateContract(userA, partnerId, ContractStatusEnum.Active, 2000m, DateTime.UtcNow),
            CreateContract(userB, partnerId, ContractStatusEnum.Active, 5000m, DateTime.UtcNow)
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

public class TestDashboardDbContext : DbContext, IContractManagementDbContext, IIdentityDbContext, IPartnerDbContext
{
    public TestDashboardDbContext(DbContextOptions<TestDashboardDbContext> options) : base(options) { }

    public DbSet<DomainContract.ContractType> ContractTypes { get; set; } = default!;
    public DbSet<DomainContract.ContractTemplateVersion> ContractTemplateVersions { get; set; } = default!;
    public DbSet<DomainContract.Contract> Contracts { get; set; } = default!;
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
        modelBuilder.Entity<DomainContract.Contract>().ToTable("CONTRACTS");
        modelBuilder.Entity<User>().ToTable("USERS");
        modelBuilder.Entity<Department>().ToTable("DEPARTMENTS");
        modelBuilder.Entity<Partner>().ToTable("PARTNERS");
    }
}
