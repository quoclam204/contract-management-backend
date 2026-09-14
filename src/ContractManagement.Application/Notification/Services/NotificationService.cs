using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Notification.Services;

/// <summary>
/// Application service for notification operations
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationDbContext _context;

    public NotificationService(INotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationRequest request)
    {
        var notification = new DomainNotification.Notification
        {
            UserId = request.UserId,
            ContractId = request.ContractId,
            Type = request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    public async Task<List<NotificationDto>> GetByUserIdAsync(Guid userId, bool? isRead = null)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<NotificationDto>> GetUnreadByUserIdAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task<int> MarkAsReadAsync(Guid userId, List<Guid> notificationIds)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && notificationIds.Contains(n.Id))
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        return await _context.SaveChangesAsync();
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        return await _context.SaveChangesAsync();
    }

    private static NotificationDto MapToDto(DomainNotification.Notification entity)
    {
        return new NotificationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ContractId = entity.ContractId,
            Type = entity.Type,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt
        };
    }
}