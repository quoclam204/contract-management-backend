using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Domain.Notification.Enums;
using DomainNotification = ContractManagement.Domain.Notification.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Contracts.Services;

/// <summary>
/// FR-08: Daily Hangfire scan. Active→Expiring when remaining ≤30, ExpiringSoon at 30/15/7,
/// recipient OwnerId, direct email, idempotent threshold-aware dedup, Hangfire retry.
/// </summary>
public class ContractExpiryJobService : IContractExpiryJobService
{
    private static readonly HashSet<int> Thresholds = new() { 30, 15, 7 };

    private readonly IContractDbContext _contracts;
    private readonly INotificationDbContext _notifications;
    private readonly IIdentityDbContext _identity;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContractExpiryJobService> _logger;

    public ContractExpiryJobService(
        IContractDbContext contracts,
        INotificationDbContext notifications,
        IIdentityDbContext identity,
        IEmailSender emailSender,
        ILogger<ContractExpiryJobService> logger)
    {
        _contracts = contracts;
        _notifications = notifications;
        _identity = identity;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<int> ProcessContractExpirationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var contracts = await _contracts.Contracts
            .Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Expiring)
            .ToListAsync(cancellationToken);

        int processed = 0;

        foreach (var contract in contracts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remainingDays = (contract.ExpiryDate.Date - today).Days;
            var isThreshold = Thresholds.Contains(remainingDays);
            var isActionable = contract.Status == ContractStatus.Active || contract.Status == ContractStatus.Expiring;
            var needsTransition = contract.Status == ContractStatus.Active && remainingDays <= 30;
            var needsNotification = isThreshold && isActionable && contract.OwnerId != Guid.Empty;

            // Idempotency / retry split:
            // - NOTIFICATIONS has no email-delivery column (C), so we dedup in-app notification by (ContractId, Type, CreatedAt UTC date).
            //   Different thresholds (30/15/7) fall on different UTC dates, so 30 on D does not suppress 15 on D+15.
            // - EMAIL is at-least-once: if notification already exists but email previously failed, retry MUST attempt email again (B/F/G).
            //   We decouple notification creation (at most once per day) from email delivery (retryable).
            //   Duplicate successful email on Hangfire retry of a later-contract failure is possible — see report (D).
            bool alreadyNotified = false;
            if (needsNotification)
            {
                alreadyNotified = await _notifications.Notifications.AnyAsync(
                    n => n.ContractId == contract.Id
                         && n.Type == NotificationType.ExpiringSoon
                         && n.CreatedAt >= today
                         && n.CreatedAt < tomorrow,
                    cancellationToken);
            }

            if (needsNotification && !alreadyNotified)
            {
                if (needsTransition)
                {
                    contract.Expire();
                    _logger.LogInformation(
                        "Transitioning contract {ContractId} Active→Expiring remaining {Remaining}",
                        contract.Id, remainingDays);
                }

                var notification = new DomainNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = contract.OwnerId,
                    ContractId = contract.Id,
                    Type = NotificationType.ExpiringSoon,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _notifications.Notifications.Add(notification);

                // Persist notification + transition before email so in-app is not lost on SMTP failure (F)
                await SaveAsync(cancellationToken);
                processed++;
            }
            else if (needsNotification && alreadyNotified)
            {
                // Already notified today — do NOT create duplicate notification (A), but still need to retry email (B).
                if (needsTransition && contract.Status == ContractStatus.Active)
                {
                    // Defensive: should already have transitioned on first execution.
                    contract.Expire();
                    await SaveAsync(cancellationToken);
                    processed++;
                    _logger.LogInformation(
                        "Transitioned contract {ContractId} Active→Expiring remaining {Remaining} (duplicate notification skipped)",
                        contract.Id, remainingDays);
                }
                else
                {
                    _logger.LogInformation(
                        "Duplicate ExpiringSoon suppressed for contract {ContractId} remaining {Remaining} (already notified today) — will still attempt email retry",
                        contract.Id, remainingDays);
                }
                // Fall through to email retry below — no continue
            }
            else if (!needsNotification && needsTransition)
            {
                // Transition-only (e.g., 20 days): Active→Expiring without notification/email
                contract.Expire();
                await SaveAsync(cancellationToken);
                processed++;
                _logger.LogInformation(
                    "Transitioned contract {ContractId} Active→Expiring remaining {Remaining} (no threshold notification)",
                    contract.Id, remainingDays);
                continue;
            }
            else
            {
                // No transition and no notification needed (e.g., 31 days, non-actionable status)
                continue;
            }

            // Email phase — independent from notification creation.
            // Runs both on first execution and on Hangfire retry when alreadyNotified==true (B/F/G).
            // Missing email is not transient: keep notification, warn, do not throw (E).
            // SMTP failure: notification remains, exception propagates for Hangfire retry (F), retry will NOT duplicate notification.
            if (needsNotification)
            {
                var owner = await _identity.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == contract.OwnerId, cancellationToken);

                var email = owner?.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning(
                        "Missing owner email for contract {ContractId} owner {OwnerId}; notification persisted, email skipped",
                        contract.Id, contract.OwnerId);
                    continue;
                }

                var subject = $"[Cảnh báo hết hạn] Hợp đồng {contract.ContractNumber} sắp hết hạn ({remainingDays} ngày)";
                var body =
                    $"Kính gửi,\n\n" +
                    $"Hợp đồng sắp hết hạn:\n" +
                    $"- Số hợp đồng: {contract.ContractNumber}\n" +
                    $"- Tiêu đề: {contract.Title}\n" +
                    $"- Ngày hết hạn: {contract.ExpiryDate:yyyy-MM-dd}\n" +
                    $"- Còn lại: {remainingDays} ngày\n\n" +
                    $"Vui lòng kiểm tra và thực hiện gia hạn hoặc thanh lý trước khi hợp đồng hết hiệu lực.\n\nTrân trọng.";

                try
                {
                    await _emailSender.SendAsync(email, subject, body, cancellationToken);
                    _logger.LogInformation(
                        "Sent expiry email for contract {ContractId} to {To} remaining {Remaining}",
                        contract.Id, email, remainingDays);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Never log SMTP password; log recipient + contract only
                    _logger.LogError(ex,
                        "Failed to send expiry email for contract {ContractId} to {To} remaining {Remaining}",
                        contract.Id, email, remainingDays);
                    // Propagate so Hangfire AutomaticRetry can retry; notification already persisted
                    // so retry will NOT create duplicate notification (alreadyNotified guard).
                    throw;
                }
            }
        }

        return processed;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        await _contracts.SaveChangesAsync(ct);
        if (!ReferenceEquals(_contracts, _notifications))
            await _notifications.SaveChangesAsync(ct);
    }
}
