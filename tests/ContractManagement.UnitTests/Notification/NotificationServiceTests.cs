using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Application.Notification.Services;
using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContractManagement.UnitTests.Notification;

public class NotificationServiceTests : IDisposable
{
    private readonly TestNotificationDbContext _context;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestNotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TestNotificationDbContext(options);
        _service = new NotificationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_CreatesNotificationWithCorrectDefaults()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            UserId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            Type = NotificationType.ApprovalRequest
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(request.UserId, result.UserId);
        Assert.Equal(request.ContractId, result.ContractId);
        Assert.Equal(request.Type, result.Type);
        Assert.False(result.IsRead);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreateAsync_WithNullContractId_CreatesNotification()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            UserId = Guid.NewGuid(),
            ContractId = null,
            Type = NotificationType.SystemAnnouncement
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(request.UserId, result.UserId);
        Assert.Null(result.ContractId);
        Assert.Equal(NotificationType.SystemAnnouncement, result.Type);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNotificationsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SignRequest, CreatedAt = DateTime.UtcNow.AddMinutes(-1) });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = otherUserId, Type = NotificationType.ApprovalRequest, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByUserIdAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal(userId, n.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithIsReadFilter_ReturnsFiltered()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SignRequest, IsRead = true, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var unread = await _service.GetByUserIdAsync(userId, false);
        var read = await _service.GetByUserIdAsync(userId, true);

        // Assert
        Assert.Single(unread);
        Assert.False(unread[0].IsRead);
        Assert.Single(read);
        Assert.True(read[0].IsRead);
    }

    [Fact]
    public async Task GetUnreadByUserIdAsync_ReturnsOnlyUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SignRequest, IsRead = true, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUnreadByUserIdAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.False(result[0].IsRead);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SignRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ExpiringSoon, IsRead = true, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetUnreadCountAsync(userId);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksSpecifiedNotificationsAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId1 = Guid.NewGuid();
        var notificationId2 = Guid.NewGuid();
        var notificationId3 = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = notificationId1, UserId = userId, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = notificationId2, UserId = userId, Type = NotificationType.SignRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = notificationId3, UserId = userId, Type = NotificationType.ExpiringSoon, IsRead = false, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var affected = await _service.MarkAsReadAsync(userId, new List<Guid> { notificationId1, notificationId2 });

        // Assert
        Assert.Equal(2, affected);
        Assert.True(_context.Notifications.First(n => n.Id == notificationId1).IsRead);
        Assert.True(_context.Notifications.First(n => n.Id == notificationId2).IsRead);
        Assert.False(_context.Notifications.First(n => n.Id == notificationId3).IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithOtherUserNotifications_OnlyUpdatesOwnNotifications()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var userANotification = Guid.NewGuid();
        var userBNotification = Guid.NewGuid();

        _context.Notifications.Add(new DomainNotification.Notification { Id = userANotification, UserId = userA, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = userBNotification, UserId = userB, Type = NotificationType.SignRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act: User A tries to mark both their own and User B's notification as read
        var affected = await _service.MarkAsReadAsync(userA, new List<Guid> { userANotification, userBNotification });

        // Assert: Only User A's notification should be updated
        Assert.Equal(1, affected);
        Assert.True(_context.Notifications.First(n => n.Id == userANotification).IsRead);
        Assert.False(_context.Notifications.First(n => n.Id == userBNotification).IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_OtherUserNotification_ExcludedFromUpdate()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var userBNotification = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = userBNotification, UserId = userB, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act: User A tries to mark User B's notification as read
        var affected = await _service.MarkAsReadAsync(userA, new List<Guid> { userBNotification });

        // Assert: No notifications updated (User B's notification remains unread)
        Assert.Equal(0, affected);
        Assert.False(_context.Notifications.First(n => n.Id == userBNotification).IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadForUserAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SignRequest, IsRead = false, CreatedAt = DateTime.UtcNow });
        _context.Notifications.Add(new DomainNotification.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ExpiringSoon, IsRead = true, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var affected = await _service.MarkAllAsReadAsync(userId);

        // Assert
        Assert.Equal(2, affected);
        Assert.All(_context.Notifications.Where(n => n.UserId == userId), n => Assert.True(n.IsRead));
    }
}

/// <summary>
/// Test implementation of INotificationDbContext using EF Core InMemory
/// </summary>
public class TestNotificationDbContext : DbContext, INotificationDbContext
{
    public TestNotificationDbContext(DbContextOptions<TestNotificationDbContext> options) : base(options) { }

    public DbSet<DomainNotification.Notification> Notifications { get; set; } = default!;

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DomainNotification.Notification>().ToTable("NOTIFICATIONS");
    }
}