using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Workflow.Services;

/// <summary>
/// Service triển khai quản lý chữ ký điện tử hợp đồng (Person 4)
/// Theo dõi trạng thái ký của các bên, khi tất cả các bên hoàn tất sẽ bắn ContractSignedEvent qua MediatR.
/// Không inject hay phụ thuộc trực tiếp vào ContractService.
/// </summary>
public class SignatureService : ISignatureService
{
    private readonly IWorkflowDbContext _context;
    private readonly IEnumerable<ISignatureProvider> _providers;
    private readonly IPublisher _publisher;
    private readonly ILogger<SignatureService> _logger;

    public SignatureService(
        IWorkflowDbContext context,
        IEnumerable<ISignatureProvider> providers,
        IPublisher publisher,
        ILogger<SignatureService> logger)
    {
        _context = context;
        _providers = providers;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<SignatureDto> SignContractAsync(CreateSignatureRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            throw new ArgumentException("Mã hợp đồng (ContractId) không hợp lệ.", nameof(request.ContractId));

        if (string.IsNullOrWhiteSpace(request.SignerName))
            throw new ArgumentException("Tên người ký không được để trống.", nameof(request.SignerName));

        // Ràng buộc tính độc quyền của người ký
        Guid signerId;
        if (request.SignerType == SignerType.InternalUser)
        {
            if (!request.InternalSignerId.HasValue || request.InternalSignerId.Value == Guid.Empty)
                throw new ArgumentException("InternalSignerId là bắt buộc khi loại người ký là Nội bộ (InternalUser).", nameof(request.InternalSignerId));

            if (request.PartnerSignerId.HasValue)
                throw new ArgumentException("PartnerSignerId phải là null khi loại người ký là Nội bộ.", nameof(request.PartnerSignerId));

            signerId = request.InternalSignerId.Value;
        }
        else if (request.SignerType == SignerType.PartnerRepresentative)
        {
            if (!request.PartnerSignerId.HasValue || request.PartnerSignerId.Value == Guid.Empty)
                throw new ArgumentException("PartnerSignerId là bắt buộc khi loại người ký là Đại diện đối tác (PartnerRepresentative).", nameof(request.PartnerSignerId));

            if (request.InternalSignerId.HasValue)
                throw new ArgumentException("InternalSignerId phải là null khi loại người ký là Đại diện đối tác.", nameof(request.InternalSignerId));

            signerId = request.PartnerSignerId.Value;
        }
        else
        {
            throw new ArgumentException("Loại người ký không hợp lệ.", nameof(request.SignerType));
        }

        // Kiểm tra xem bên này đã ký hợp đồng chưa
        var existingSignatures = await _context.Signatures
            .Where(s => s.ContractId == request.ContractId)
            .ToListAsync(cancellationToken);

        if (existingSignatures.Any(s => s.SignerType == request.SignerType))
        {
            throw new InvalidOperationException($"Bên ký '{request.SignerType}' đã hoàn tất chữ ký cho hợp đồng này. Không thể ký lặp lại.");
        }

        // Tìm nhà cung cấp chữ ký phù hợp
        var provider = _providers.FirstOrDefault(p => p.Method == request.SignatureMethod);
        if (provider == null)
        {
            throw new NotSupportedException($"Phương thức ký '{request.SignatureMethod}' chưa được triển khai hoặc không hỗ trợ.");
        }

        var signDocRequest = new SignDocumentRequest
        {
            ContractId = request.ContractId,
            SignerId = signerId,
            SignerName = request.SignerName.Trim(),
            SignerType = request.SignerType,
            OtpCode = request.OtpCode,
            Timestamp = DateTime.UtcNow
        };

        var signResult = await provider.SignAsync(signDocRequest, cancellationToken);
        if (!signResult.Success)
        {
            throw new InvalidOperationException(signResult.ErrorMessage ?? "Ký điện tử thất bại do lỗi không xác định từ nhà cung cấp.");
        }

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            ContractId = request.ContractId,
            SignerType = request.SignerType,
            InternalSignerId = request.InternalSignerId,
            PartnerSignerId = request.PartnerSignerId,
            SignerNameSnapshot = request.SignerName.Trim(),
            SignatureMethod = request.SignatureMethod,
            SignedAt = signDocRequest.Timestamp,
            SignatureHash = signResult.SignatureHash
        };

        _context.Signatures.Add(signature);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ghi nhận thành công chữ ký {SignatureId} ({SignerType}) cho ContractId: {ContractId} bằng phương thức {Method}",
            signature.Id, signature.SignerType, signature.ContractId, signature.SignatureMethod);

        // Kiểm tra xem tất cả các bên yêu cầu đã hoàn tất việc ký chưa (InternalUser & PartnerRepresentative)
        var allSignatures = existingSignatures.Concat(new[] { signature }).ToList();
        var hasInternal = allSignatures.Any(s => s.SignerType == SignerType.InternalUser);
        var hasPartner = allSignatures.Any(s => s.SignerType == SignerType.PartnerRepresentative);

        if (hasInternal && hasPartner)
        {
            _logger.LogInformation("TẤT CẢ các bên theo yêu cầu (Nội bộ & Đối tác) đã ký hoàn tất hợp đồng {ContractId}. Bắn sự kiện ContractSignedEvent qua MediatR.",
                request.ContractId);

            // Bắn sự kiện để module Contract bắt và cập nhật trạng thái hợp đồng sang Signed
            await _publisher.Publish(new ContractSignedEvent(request.ContractId), cancellationToken);
        }
        else
        {
            _logger.LogInformation("Hợp đồng {ContractId} đã có {Count}/2 bên ký kết. Đang chờ bên còn lại.",
                request.ContractId, allSignatures.Count);
        }

        return MapToDto(signature);
    }

    public async Task<List<SignatureDto>> GetSignaturesByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var list = await _context.Signatures
            .Where(s => s.ContractId == contractId)
            .OrderBy(s => s.SignedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<ContractSignatureStatusDto> GetSignatureStatusAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var signatures = await _context.Signatures
            .Where(s => s.ContractId == contractId)
            .OrderBy(s => s.SignedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasInternal = signatures.Any(s => s.SignerType == SignerType.InternalUser);
        var hasPartner = signatures.Any(s => s.SignerType == SignerType.PartnerRepresentative);

        return new ContractSignatureStatusDto
        {
            ContractId = contractId,
            HasInternalSignature = hasInternal,
            HasPartnerSignature = hasPartner,
            IsFullySigned = hasInternal && hasPartner,
            TotalRequiredParties = 2,
            SignedPartiesCount = signatures.Count,
            Signatures = signatures.Select(MapToDto).ToList()
        };
    }

    private static SignatureDto MapToDto(Signature s)
    {
        return new SignatureDto
        {
            Id = s.Id,
            ContractId = s.ContractId,
            SignerType = s.SignerType,
            InternalSignerId = s.InternalSignerId,
            PartnerSignerId = s.PartnerSignerId,
            SignerNameSnapshot = s.SignerNameSnapshot,
            SignatureMethod = s.SignatureMethod,
            SignedAt = s.SignedAt,
            SignatureHash = s.SignatureHash
        };
    }
}
