using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Workflow.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Contracts.Handlers;

/// <summary>
/// Xử lý sự kiện WorkflowRejectedEvent từ Workflow module:
/// Tự động cập nhật trạng thái hợp đồng về Draft (0) theo State Machine và SRS
/// để tác giả có thể chỉnh sửa nội dung và đệ trình phê duyệt lại.
/// </summary>
public class WorkflowRejectedEventHandler : INotificationHandler<WorkflowRejectedEvent>
{
    private readonly IContractDbContext _context;
    private readonly ILogger<WorkflowRejectedEventHandler> _logger;

    public WorkflowRejectedEventHandler(
        IContractDbContext context,
        ILogger<WorkflowRejectedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(WorkflowRejectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Nhận sự kiện WorkflowRejectedEvent cho ContractId: {ContractId}. Lý do: {Reason}",
            notification.ContractId, notification.Reason);

        var contract = await _context.Contracts.FindAsync(new object[] { notification.ContractId }, cancellationToken);
        if (contract == null)
        {
            _logger.LogError("Không tìm thấy hợp đồng {ContractId} khi xử lý WorkflowRejectedEvent", notification.ContractId);
            return;
        }

        // Chuyển trạng thái về Draft thông qua domain method
        contract.Reject(notification.Reason);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã chuyển hợp đồng {ContractId} về trạng thái Draft ({Status}) để chỉnh sửa sau khi bị từ chối phê duyệt",
            contract.Id, contract.Status);
    }
}
