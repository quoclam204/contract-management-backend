using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Application.Contract.Services;
using ContractManagement.Application.Notification.Interfaces;
using DomainContract = ContractManagement.Domain.Contract.Entities;
using ContractManagement.Domain.Contract.Enums;
using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Contract;

public class ContractExpiryJobServiceTests : IDisposable
{
    private readonly TestContractDbContext _context;
    private readonly ContractExpiryJobService _service;

    public ContractExpiryJobServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContractDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContractDbContext(options);
        _service = new ContractExpiryJobService(
            _context,
            _context,
            NullLogger<ContractExpiryJobService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static DomainContract.Contract CreateTestContract(
        string contractNumber,
        string title,
        Guid ownerId,
        DateTime expiryDate,
        ContractStatusEnum status)
    {
        return new DomainContract.Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = contractNumber,
            Title = title,
            OwnerId = ownerId,
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            EffectiveDate = DateTime.UtcNow.Date.AddYears(-1),
            ExpiryDate = expiryDate,
            Status = (byte)status,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_With30DaysRemaining_TransitionsStatusAndCreatesNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var contract = CreateTestContract("CTR-30DAYS", "30 Days Contract", ownerId, today.AddDays(30), ContractStatusEnum.Active);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessContractExpirationsAsync();

        // Assert
        Assert.Equal(1, result);
        var updatedContract = await _context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal((byte)ContractStatusEnum.Expiring, updatedContract.Status);

        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.ContractId == contract.Id);
        Assert.NotNull(notification);
        Assert.Equal(ownerId, notification.UserId);
        Assert.Equal(NotificationType.ExpiringSoon, notification.Type);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_With15DaysRemaining_TransitionsStatusAndCreatesNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var contract = CreateTestContract("CTR-15DAYS", "15 Days Contract", ownerId, today.AddDays(15), ContractStatusEnum.Active);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessContractExpirationsAsync();

        // Assert
        Assert.Equal(1, result);
        var updatedContract = await _context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal((byte)ContractStatusEnum.Expiring, updatedContract.Status);

        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.ContractId == contract.Id);
        Assert.NotNull(notification);
        Assert.Equal(ownerId, notification.UserId);
        Assert.Equal(NotificationType.ExpiringSoon, notification.Type);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_With7DaysRemaining_TransitionsStatusAndCreatesNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var contract = CreateTestContract("CTR-7DAYS", "7 Days Contract", ownerId, today.AddDays(7), ContractStatusEnum.Active);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessContractExpirationsAsync();

        // Assert
        Assert.Equal(1, result);
        var updatedContract = await _context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal((byte)ContractStatusEnum.Expiring, updatedContract.Status);

        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.ContractId == contract.Id);
        Assert.NotNull(notification);
        Assert.Equal(ownerId, notification.UserId);
        Assert.Equal(NotificationType.ExpiringSoon, notification.Type);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_OutsideWarningThresholds_DoesNotCreateNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;

        // 40 days remaining: Status Active -> should stay Active, no notification
        var contract40Days = CreateTestContract("CTR-40DAYS", "40 Days Contract", ownerId, today.AddDays(40), ContractStatusEnum.Active);

        // 20 days remaining: Status Active -> transitions to Expiring (<= 30 days), but no notification (20 not in 30,15,7)
        var contract20Days = CreateTestContract("CTR-20DAYS", "20 Days Contract", ownerId, today.AddDays(20), ContractStatusEnum.Active);

        _context.Contracts.AddRange(contract40Days, contract20Days);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessContractExpirationsAsync();

        // Assert
        Assert.Equal(2, result);

        var updated40 = await _context.Contracts.FindAsync(contract40Days.Id);
        Assert.NotNull(updated40);
        Assert.Equal((byte)ContractStatusEnum.Active, updated40.Status);

        var updated20 = await _context.Contracts.FindAsync(contract20Days.Id);
        Assert.NotNull(updated20);
        Assert.Equal((byte)ContractStatusEnum.Expiring, updated20.Status);

        // No notifications created for 40 or 20 days
        var notifications = await _context.Notifications.ToListAsync();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_ContractNotActiveOrExpiring_Ignored()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;

        var draftContract = CreateTestContract("CTR-DRAFT", "Draft Contract", ownerId, today.AddDays(15), ContractStatusEnum.Draft);
        var terminatedContract = CreateTestContract("CTR-TERMINATED", "Terminated Contract", ownerId, today.AddDays(15), ContractStatusEnum.Terminated);

        _context.Contracts.AddRange(draftContract, terminatedContract);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessContractExpirationsAsync();

        // Assert
        Assert.Equal(0, result);

        var updatedDraft = await _context.Contracts.FindAsync(draftContract.Id);
        Assert.NotNull(updatedDraft);
        Assert.Equal((byte)ContractStatusEnum.Draft, updatedDraft.Status);

        var updatedTerminated = await _context.Contracts.FindAsync(terminatedContract.Id);
        Assert.NotNull(updatedTerminated);
        Assert.Equal((byte)ContractStatusEnum.Terminated, updatedTerminated.Status);

        var notifications = await _context.Notifications.ToListAsync();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_DuplicateRun_DoesNotCreateDuplicateNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var contract = CreateTestContract("CTR-DUPLICATE-TEST", "Duplicate Test Contract", ownerId, today.AddDays(30), ContractStatusEnum.Active);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act 1: First run
        await _service.ProcessContractExpirationsAsync();

        var countAfterFirstRun = await _context.Notifications.CountAsync(n => n.ContractId == contract.Id);
        Assert.Equal(1, countAfterFirstRun);

        // Act 2: Second run on same day
        await _service.ProcessContractExpirationsAsync();

        // Assert: No second notification created
        var countAfterSecondRun = await _context.Notifications.CountAsync(n => n.ContractId == contract.Id);
        Assert.Equal(1, countAfterSecondRun);
    }

    [Fact]
    public async Task ProcessContractExpirationsAsync_ValidOwnerId_NotificationHasCorrectOwnerAndType()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var contract = CreateTestContract("CTR-OWNER-TEST", "Owner Test Contract", ownerId, today.AddDays(15), ContractStatusEnum.Active);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act
        await _service.ProcessContractExpirationsAsync();

        // Assert
        var notification = await _context.Notifications.SingleOrDefaultAsync(n => n.ContractId == contract.Id);
        Assert.NotNull(notification);
        Assert.Equal(ownerId, notification.UserId);
        Assert.Equal(contract.Id, notification.ContractId);
        Assert.Equal(NotificationType.ExpiringSoon, notification.Type);
    }
}

public class TestContractDbContext : DbContext, IContractManagementDbContext, INotificationDbContext
{
    public TestContractDbContext(DbContextOptions<TestContractDbContext> options) : base(options) { }

    public DbSet<DomainContract.ContractType> ContractTypes { get; set; } = default!;
    public DbSet<DomainContract.ContractTemplateVersion> ContractTemplateVersions { get; set; } = default!;
    public DbSet<DomainContract.Contract> Contracts { get; set; } = default!;
    public DbSet<DomainNotification.Notification> Notifications { get; set; } = default!;
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    public DbSet<WorkflowStep> WorkflowSteps { get; set; } = default!;
    public DbSet<ApprovalStep> ApprovalSteps { get; set; } = default!;

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
        modelBuilder.Entity<DomainNotification.Notification>().ToTable("NOTIFICATIONS");
    }
}
