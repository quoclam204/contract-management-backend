using DomainNotification = ContractManagement.Domain.Notification.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Notification.Interfaces;

/// <summary>
/// Database context abstraction for notification persistence
/// Mirrors the pattern used by IWorkflowDbContext
/// </summary>
public interface INotificationDbContext
{
    DbSet<DomainNotification.Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}