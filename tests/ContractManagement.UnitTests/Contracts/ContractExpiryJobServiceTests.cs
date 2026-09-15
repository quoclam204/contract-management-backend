using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contracts.Services;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Application.Notification.Interfaces;
using CanonicalContract = ContractManagement.Domain.Contracts.Entities.Contract;
using CanonicalContractType = ContractManagement.Domain.Contracts.Entities.ContractType;
using CanonicalContractTemplateVersion = ContractManagement.Domain.Contracts.Entities.ContractTemplateVersion;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DomainNotification = ContractManagement.Domain.Notification.Entities.Notification;

namespace ContractManagement.UnitTests.Contracts;

public class ContractExpiryJobServiceTests : IDisposable
{
    private readonly TestExpiryDbContext _context;
    private readonly FakeEmailSender _emailSender;
    private readonly IContractExpiryJobService _sut;

    public ContractExpiryJobServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestExpiryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestExpiryDbContext(options);
        _emailSender = new FakeEmailSender();
        _sut = new ContractExpiryJobService(_context, _context, _context, _emailSender, NullLogger<ContractExpiryJobService>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private static CanonicalContract CreateContract(Guid id, Guid ownerId, ContractStatus status, DateTime expiryDate)
    {
        return new CanonicalContract
        {
            Id = id,
            ContractNumber = "HD-" + id.ToString()[..8],
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Test Contract " + id.ToString()[..4],
            Value = 1000m,
            EffectiveDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = expiryDate,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        };
    }

    private async Task<User> CreateOwnerAsync(Guid ownerId, string email)
    {
        var user = new User
        {
            Id = ownerId,
            FullName = "Owner " + ownerId.ToString()[..4],
            Email = email,
            PasswordHash = "hash",
            Role = UserRole.Staff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Active_At30Days_BecomesExpiring_CreatesNotification_SendsEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner30@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(30));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        var count = await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Expiring, updated!.Status);
        var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.ContractId == contract.Id);
        Assert.NotNull(notif);
        Assert.Equal(NotificationType.ExpiringSoon, notif!.Type);
        Assert.Equal(ownerId, notif.UserId);
        Assert.Single(_emailSender.Sent);
        Assert.Equal("owner30@test.com", _emailSender.Sent[0].To);
        Assert.Contains(contract.ContractNumber, _emailSender.Sent[0].Subject);
        Assert.Contains("30", _emailSender.Sent[0].Body);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Active_At15Days_BecomesExpiring_CreatesNotification_SendsEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner15@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(15));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Expiring, updated!.Status);
        var notif = await _context.Notifications.FirstAsync(n => n.ContractId == contract.Id);
        Assert.Equal(NotificationType.ExpiringSoon, notif.Type);
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task Active_At7Days_BecomesExpiring_CreatesNotification_SendsEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner7@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Expiring, updated!.Status);
        Assert.Single(_emailSender.Sent);
        var notif = await _context.Notifications.FirstAsync(n => n.ContractId == contract.Id);
        Assert.Equal(NotificationType.ExpiringSoon, notif.Type);
    }

    [Fact]
    public async Task Active_At31Days_RemainsActive_NoNotification_NoEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner31@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(31));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Active, updated!.Status);
        Assert.Empty(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task Active_At20Days_TransitionsToExpiring_NoNotification_NoEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner20@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(20));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Expiring, updated!.Status);
        Assert.Empty(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task Expiring_At15Days_RemainsExpiring_SendsNotificationAndEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-exp15@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Expiring, DateTime.UtcNow.Date.AddDays(15));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Expiring, updated!.Status);
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task Expiring_At7Days_SendsNotificationAndEmail()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-exp7@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Expiring, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Single(_emailSender.Sent);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    public async Task NonActionableStatuses_NoTransition_NoNotification_NoEmail(ContractStatus status)
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-non@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, status, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated = await _context.Contracts.FindAsync(contract.Id);
        Assert.Equal(status, updated!.Status);
        Assert.Empty(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task DuplicateExecution_SameThreshold_DoesNotCreateDuplicateNotification_ButResendsEmail_AtLeastOnce()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-dup@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(30));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Single(_emailSender.Sent);

        // Second run same UTC date same threshold — notification deduped but email is retryable (at-least-once, see report D).
        // Without an email-delivery status column we cannot know if the first email succeeded, so we resend.
        await _sut.ProcessContractExpirationsAsync();
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Equal(2, _emailSender.Sent.Count);
    }

    [Fact]
    public async Task SuccessfulEmail_DoesNotCreateDuplicateNotification()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-success-dup@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();
        var countBefore = _context.Notifications.Count(n => n.ContractId == contract.Id);
        Assert.Equal(1, countBefore);

        _emailSender.Sent.Clear();
        await _sut.ProcessContractExpirationsAsync();

        // Notification idempotent — no second row (A)
        Assert.Equal(1, _context.Notifications.Count(n => n.ContractId == contract.Id));
        // Email is at-least-once without delivery column — second run resends (D)
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task DifferentThresholds_30DoesNotSuppress15()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-diff@test.com");
        var contractId = Guid.NewGuid();
        var contract = CreateContract(contractId, ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(30));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contractId));

        // Simulate 15 days passing: move notification to 15 days ago (yesterday) so today is not duplicate
        var first = await _context.Notifications.FirstAsync(n => n.ContractId == contractId);
        first.CreatedAt = DateTime.UtcNow.AddDays(-15);
        await _context.SaveChangesAsync();

        // Update expiry to 15 days remaining
        var c = await _context.Contracts.FindAsync(contractId);
        c!.ExpiryDate = DateTime.UtcNow.Date.AddDays(15);
        await _context.SaveChangesAsync();

        _emailSender.Sent.Clear();
        await _sut.ProcessContractExpirationsAsync();

        Assert.Equal(2, _context.Notifications.Count(n => n.ContractId == contractId));
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task DifferentThresholds_15DoesNotSuppress7()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "owner-diff2@test.com");
        var contractId = Guid.NewGuid();
        var contract = CreateContract(contractId, ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(15));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contractId));

        var first = await _context.Notifications.FirstAsync(n => n.ContractId == contractId);
        first.CreatedAt = DateTime.UtcNow.AddDays(-8);
        await _context.SaveChangesAsync();

        var c = await _context.Contracts.FindAsync(contractId);
        c!.ExpiryDate = DateTime.UtcNow.Date.AddDays(7);
        await _context.SaveChangesAsync();

        _emailSender.Sent.Clear();
        await _sut.ProcessContractExpirationsAsync();

        Assert.Equal(2, _context.Notifications.Count(n => n.ContractId == contractId));
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task CorrectOwnerId_Recipient()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "correct@test.com");
        await CreateOwnerAsync(otherId, "other@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var notif = await _context.Notifications.FirstAsync(n => n.ContractId == contract.Id);
        Assert.Equal(ownerId, notif.UserId);
        Assert.NotEqual(otherId, notif.UserId);
    }

    [Fact]
    public async Task CorrectNotificationType_IsExpiringSoon()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "type@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var notif = await _context.Notifications.FirstAsync(n => n.ContractId == contract.Id);
        Assert.Equal(NotificationType.ExpiringSoon, notif.Type);
    }

    [Fact]
    public async Task MissingOwnerEmail_NotificationPersisted_EmailSkipped()
    {
        var ownerId = Guid.NewGuid();
        // Owner not in Users -> email missing
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        var count = await _sut.ProcessContractExpirationsAsync();

        // Notification persisted even though email missing
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Empty(_emailSender.Sent);
        Assert.Equal(1, count);
        // No exception thrown for missing email
    }

    [Fact]
    public async Task MissingOwnerEmail_EmptyEmail_NotificationPersisted()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, ""); // empty email
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task EmailFailure_ExceptionPropagates_NotificationNotDuplicatedOnRetry()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "fail@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _emailSender.ShouldThrow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessContractExpirationsAsync());

        // Notification was persisted before email failure (F)
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        var notifCountBeforeRetry = _context.Notifications.Count(n => n.ContractId == contract.Id);

        // Fix email and retry — notification deduped (same UTC date) but email retried (G)
        _emailSender.ShouldThrow = false;
        _emailSender.Sent.Clear();
        await _sut.ProcessContractExpirationsAsync();

        Assert.Equal(notifCountBeforeRetry, _context.Notifications.Count(n => n.ContractId == contract.Id));
        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task EmailFailure_PersistsNotification()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "fail-persists@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _emailSender.ShouldThrow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessContractExpirationsAsync());

        // F: notification must remain persisted even though SMTP failed
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
        var notif = await _context.Notifications.SingleAsync(n => n.ContractId == contract.Id);
        Assert.Equal(NotificationType.ExpiringSoon, notif.Type);
        Assert.Equal(ownerId, notif.UserId);
    }

    [Fact]
    public async Task EmailFailure_RetryAttemptsEmailAgain()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "fail-retry@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _emailSender.ShouldThrow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessContractExpirationsAsync());
        Assert.Empty(_emailSender.Sent); // failed send does not record

        // G: retry with email fixed must attempt delivery again
        _emailSender.ShouldThrow = false;
        await _sut.ProcessContractExpirationsAsync();

        Assert.Single(_emailSender.Sent);
        Assert.Equal("fail-retry@test.com", _emailSender.Sent[0].To);
    }

    [Fact]
    public async Task EmailFailure_RetryDoesNotCreateDuplicateNotification()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "fail-nodup@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _emailSender.ShouldThrow = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessContractExpirationsAsync());
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));

        _emailSender.ShouldThrow = false;
        _emailSender.Sent.Clear();
        await _sut.ProcessContractExpirationsAsync();

        // F/G: retry must NOT create second notification
        Assert.Single(_context.Notifications.Where(n => n.ContractId == contract.Id));
    }

    [Fact]
    public async Task CancellationToken_IsRespected()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.ProcessContractExpirationsAsync(cts.Token));
    }

    [Fact]
    public async Task EmailBody_ContainsRequiredFields()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "body@test.com");
        var contract = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(7));
        contract.Title = "Hợp đồng quan trọng";
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Contains(contract.ContractNumber, sent.Subject);
        Assert.Contains(contract.ContractNumber, sent.Body);
        Assert.Contains("Hợp đồng quan trọng", sent.Body);
        Assert.Contains(contract.ExpiryDate.ToString("yyyy-MM-dd"), sent.Body);
        Assert.Contains("7", sent.Body);
    }

    [Fact]
    public async Task NoThresholdDays_OnlyTransitions_29And14DoNotNotify()
    {
        var ownerId = Guid.NewGuid();
        await CreateOwnerAsync(ownerId, "notrans@test.com");
        var c29 = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(29));
        var c14 = CreateContract(Guid.NewGuid(), ownerId, ContractStatus.Active, DateTime.UtcNow.Date.AddDays(14));
        _context.Contracts.AddRange(c29, c14);
        await _context.SaveChangesAsync();

        await _sut.ProcessContractExpirationsAsync();

        var updated29 = await _context.Contracts.FindAsync(c29.Id);
        var updated14 = await _context.Contracts.FindAsync(c14.Id);
        Assert.Equal(ContractStatus.Expiring, updated29!.Status); // 29 <=30 so transitions
        Assert.Equal(ContractStatus.Expiring, updated14!.Status);
        // No threshold notifications
        Assert.Empty(_context.Notifications.Where(n => n.ContractId == c29.Id));
        Assert.Empty(_context.Notifications.Where(n => n.ContractId == c14.Id));
        Assert.Empty(_emailSender.Sent);
    }
}

internal class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();
    public bool ShouldThrow { get; set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new InvalidOperationException("SMTP failure (fake)");
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

internal class TestExpiryDbContext : DbContext, IContractDbContext, INotificationDbContext, IIdentityDbContext
{
    public TestExpiryDbContext(DbContextOptions<TestExpiryDbContext> options) : base(options) { }
    public DbSet<CanonicalContract> Contracts { get; set; } = default!;
    public DbSet<CanonicalContractType> ContractTypes { get; set; } = default!;
    public DbSet<CanonicalContractTemplateVersion> ContractTemplateVersions { get; set; } = default!;
    public DbSet<DomainNotification> Notifications { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Department> Departments { get; set; } = default!;
    DbSet<ContractManagement.Domain.Identity.Entities.Department> IIdentityDbContext.Departments => Departments;
    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => base.SaveChangesAsync(cancellationToken);
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<CanonicalContract>().ToTable("CONTRACTS");
        b.Entity<CanonicalContractType>().ToTable("CONTRACT_TYPES");
        b.Entity<CanonicalContractTemplateVersion>().ToTable("CONTRACT_TEMPLATE_VERSIONS");
        b.Entity<DomainNotification>().ToTable("NOTIFICATIONS");
        b.Entity<User>().ToTable("USERS");
        b.Entity<Department>().ToTable("DEPARTMENTS");
    }
}
