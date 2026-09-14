using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Workflow.Events;
using MediatR;
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
    private readonly ILogger<WorkflowApprovedEventHandler> _logger;

    public WorkflowApprovedEventHandler(
        IContractDbContext context,
        IMediator mediator,
        ILogger<WorkflowApprovedEventHandler> logger)
    {
        _context = context;
        _mediator = mediator;
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

        // Bắn sự kiện nội bộ ContractApprovedEvent
        await _mediator.Publish(new ContractApprovedEvent(contract.Id), cancellationToken);
    }
}
