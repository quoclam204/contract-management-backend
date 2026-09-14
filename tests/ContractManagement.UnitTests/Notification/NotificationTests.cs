using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using Xunit;

namespace ContractManagement.UnitTests.Notification;

public class NotificationTests
{
    [Fact]
    public void Notification_CanBeCreated_WithValidRequiredValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var type = NotificationType.ApprovalRequest;

        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = userId,
            ContractId = contractId,
            Type = type
        };

        // Assert
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(contractId, notification.ContractId);
        Assert.Equal(type, notification.Type);
    }

    [Fact]
    public void Notification_Id_IsInitialized_WithNewGuid()
    {
        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.ApprovalRequest
        };

        // Assert
        Assert.NotEqual(Guid.Empty, notification.Id);
    }

    [Fact]
    public void Notification_IsRead_DefaultsToFalse()
    {
        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.ApprovalRequest
        };

        // Assert
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Notification_ContractId_CanBeNull()
    {
        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.SystemAnnouncement,
            ContractId = null
        };

        // Assert
        Assert.Null(notification.ContractId);
    }

    [Theory]
    [InlineData(NotificationType.ApprovalRequest, 0)]
    [InlineData(NotificationType.SignRequest, 1)]
    [InlineData(NotificationType.ExpiringSoon, 2)]
    [InlineData(NotificationType.SystemAnnouncement, 3)]
    public void NotificationType_Values_MatchDatabaseCheckConstraint(NotificationType type, byte expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (byte)type);
    }

    [Fact]
    public void Notification_CreatedAt_FollowsUtcNowConvention()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.ApprovalRequest
        };

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(notification.CreatedAt >= before && notification.CreatedAt <= after,
            $"CreatedAt {notification.CreatedAt} should be between {before} and {after}");
    }

    [Fact]
    public void Notification_CanBeCreated_WithSystemAnnouncementType_AndNullContractId()
    {
        // Act
        var notification = new DomainNotification.Notification
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.SystemAnnouncement,
            ContractId = null
        };

        // Assert
        Assert.Equal(NotificationType.SystemAnnouncement, notification.Type);
        Assert.Null(notification.ContractId);
        Assert.False(notification.IsRead);
        Assert.NotEqual(Guid.Empty, notification.Id);
    }
}