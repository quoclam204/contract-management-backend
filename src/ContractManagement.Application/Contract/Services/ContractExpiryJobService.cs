using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Domain.Contract.Enums;
using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Contract.Services;

/// <summary>
/// Background job service that checks for expiring contracts,
/// updates status from Active to Expiring when remaining days &lt;= 30,
/// and sends ExpiringSoon notifications at 30, 15, and 7-day warning thresholds.
/// </summary>
public class ContractExpiryJobService : IContractExpiryJobService
{
    private static readonly int[] WarningThresholds = { 30, 15, 7 };

    private readonly IContractManagementDbContext _contractDbContext;
    private readonly INotificationDbContext _notificationDbContext;
    private readonly ILogger<ContractExpiryJobService> _logger;

    public ContractExpiryJobService(
        IContractManagementDbContext contractDbContext,
        INotificationDbContext notificationDbContext,
        ILogger<ContractExpiryJobService> logger)
    {
        _contractDbContext = contractDbContext;
        _notificationDbContext = notificationDbContext;
        _logger = logger;
    }

    public async Task<int> ProcessContractExpirationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting contract expiry scan background job.");

        var today = DateTime.UtcNow.Date;

        // Fetch candidate contracts in Active or Expiring status
        var candidateContracts = await _contractDbContext.Contracts
            .Where(c => c.Status == (byte)ContractStatusEnum.Active || c.Status == (byte)ContractStatusEnum.Expiring)
            .ToListAsync(cancellationToken);

        int processedCount = 0;

        foreach (var contract in candidateContracts)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Contract expiry processing was cancelled.");
                break;
            }

            var remainingDays = (contract.ExpiryDate.Date - today).Days;

            // State Machine Transition: Active (4) -> Expiring (5) when remaining days <= 30
            if (contract.Status == (byte)ContractStatusEnum.Active && remainingDays <= 30)
            {
                contract.Status = (byte)ContractStatusEnum.Expiring;
                contract.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Contract {ContractId} ({ContractNumber}) status updated from Active to Expiring (Remaining days: {RemainingDays}).",
                    contract.Id, contract.ContractNumber, remainingDays);
            }

            // Warning Threshold Check: 30, 15, 7 days
            if (WarningThresholds.Contains(remainingDays))
            {
                if (contract.OwnerId != Guid.Empty)
                {
                    // Duplicate prevention: check if ExpiringSoon notification for this contract already created today
                    var existsToday = await _notificationDbContext.Notifications
                        .AnyAsync(n => n.ContractId == contract.Id
                                    && n.Type == NotificationType.ExpiringSoon
                                    && n.CreatedAt.Date == today, cancellationToken);

                    if (!existsToday)
                    {
                        var notification = new DomainNotification.Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = contract.OwnerId,
                            ContractId = contract.Id,
                            Type = NotificationType.ExpiringSoon,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        _notificationDbContext.Notifications.Add(notification);
                        _logger.LogInformation("Created ExpiringSoon notification for Owner {OwnerId} on Contract {ContractId} ({ContractNumber}) at threshold {RemainingDays} days.",
                            contract.OwnerId, contract.Id, contract.ContractNumber, remainingDays);
                    }
                    else
                    {
                        _logger.LogInformation("ExpiringSoon notification already exists for Contract {ContractId} today. Skipping duplicate.", contract.Id);
                    }
                }
            }

            processedCount++;
        }

        await _contractDbContext.SaveChangesAsync(cancellationToken);

        // If DbContext instances are distinct in testing/mock scenarios, save notification context as well
        if (!ReferenceEquals(_contractDbContext, _notificationDbContext))
        {
            await _notificationDbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Completed contract expiry scan. Processed {Count} contracts.", processedCount);
        return processedCount;
    }
}
