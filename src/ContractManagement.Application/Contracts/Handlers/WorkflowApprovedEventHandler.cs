using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Application.Workflow.Events;
using ContractManagement.Domain.Notification.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Contracts.Handlers;

/// <summary>
/// Xử lý sự kiện WorkflowApprovedEvent từ Workflow module:
/// Tự động cập nhật trạng thái hợp đồng sang Approved (2) theo State Machine.
/// </summary>
public class WorkflowApprovedEventHandler : INotificationHandler<WorkflowApprovedEvent>
{
    private readonly IContractDbContext _context;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly INotificationDbContext _notifications;
    private readonly ILogger<WorkflowApprovedEventHandler> _logger;

    public WorkflowApprovedEventHandler(
        IContractDbContext context,
        IMediator mediator,
        INotificationService notificationService,
        INotificationDbContext notifications,
        ILogger<WorkflowApprovedEventHandler> logger)
    {
        _context = context;
        _mediator = mediator;
        _notificationService = notificationService;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task Handle(WorkflowApprovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Nhận sự kiện WorkflowApprovedEvent cho ContractId: {ContractId}", notification.ContractId);

        var contract = await _context.Contracts.FindAsync(new object[] { notification.ContractId }, cancellationToken);
        if (contract == null)
        {
            _logger.LogError("Không tìm thấy hợp đồng {ContractId} khi xử lý WorkflowApprovedEvent", notification.ContractId);
            return;
        }

        // Chuyển trạng thái sang Approved thông qua domain method
        contract.Approve();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cập nhật trạng thái hợp đồng {ContractId} sang Approved ({Status}) thành công",
            contract.Id, contract.Status);

        // MVP synchronous SignRequest — canonical final-approval point, after Approve+SaveChanges, before ContractApprovedEvent
        // No authoritative signer mapping exists; use Contract.OwnerId as MVP-safe internal signer
        if (contract.OwnerId != Guid.Empty)
        {
            bool alreadyExists = await _notifications.Notifications.AnyAsync(
                n => n.ContractId == contract.Id
                     && n.UserId == contract.OwnerId
                     && n.Type == NotificationType.SignRequest,
                cancellationToken);

            if (!alreadyExists)
            {
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        UserId = contract.OwnerId,
                        ContractId = contract.Id,
                        Type = NotificationType.SignRequest
                    });
                    _logger.LogInformation("Created SignRequest notification for Contract {ContractId} Owner {OwnerId}", contract.Id, contract.OwnerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create SignRequest notification for Contract {ContractId} Owner {OwnerId}", contract.Id, contract.OwnerId);
                }
            }
            else
            {
                _logger.LogInformation("SignRequest already exists for Contract {ContractId} Owner {OwnerId} — skipping duplicate", contract.Id, contract.OwnerId);
            }
        }

        // Bắn sự kiện nội bộ ContractApprovedEvent
        await _mediator.Publish(new ContractApprovedEvent(contract.Id), cancellationToken);
    }
}
