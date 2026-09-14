using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Domain.Contracts.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Contracts.Handlers;

/// <summary>
/// Xử lý sự kiện ContractSignedEvent khi tất cả các bên đã ký thành công:
/// Tự động cập nhật trạng thái hợp đồng sang Signed (3) theo State Machine.
/// </summary>
public class ContractSignedEventHandler : INotificationHandler<ContractSignedEvent>
{
    private readonly IContractDbContext _context;
    private readonly ILogger<ContractSignedEventHandler> _logger;

    public ContractSignedEventHandler(
        IContractDbContext context,
        ILogger<ContractSignedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(ContractSignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Nhận sự kiện ContractSignedEvent cho ContractId: {ContractId}", notification.ContractId);

        var contract = await _context.Contracts.FindAsync(new object[] { notification.ContractId }, cancellationToken);
        if (contract == null)
        {
            _logger.LogError("Không tìm thấy hợp đồng {ContractId} khi xử lý ContractSignedEvent", notification.ContractId);
            return;
        }

        if (contract.Status == ContractStatus.Approved)
        {
            contract.Sign(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã chuyển hợp đồng {ContractId} sang trạng thái Signed ({Status}) sau khi tất cả các bên ký hoàn tất",
                contract.Id, contract.Status);
        }
        else
        {
            _logger.LogWarning("Hợp đồng {ContractId} đang ở trạng thái {Status}, bỏ qua bước chuyển trạng thái Signed",
                contract.Id, contract.Status);
        }
    }
}
