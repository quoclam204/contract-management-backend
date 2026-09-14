using ContractManagement.Application.Workflow.DTOs;

namespace ContractManagement.Application.Workflow.Interfaces;

/// <summary>
/// Service quản lý nghiệp vụ ký kết hợp đồng điện tử (Signature Use Cases)
/// </summary>
public interface ISignatureService
{
    /// <summary>
    /// Thực hiện ký hợp đồng cho một bên (InternalUser hoặc PartnerRepresentative)
    /// </summary>
    Task<SignatureDto> SignContractAsync(CreateSignatureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các chữ ký đã ghi nhận của hợp đồng
    /// </summary>
    Task<List<SignatureDto>> GetSignaturesByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy trạng thái tiến độ ký kết của các bên đối với hợp đồng
    /// </summary>
    Task<ContractSignatureStatusDto> GetSignatureStatusAsync(Guid contractId, CancellationToken cancellationToken = default);
}
