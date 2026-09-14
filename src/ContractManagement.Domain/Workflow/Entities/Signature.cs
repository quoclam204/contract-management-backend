using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Domain.Workflow.Entities;

/// <summary>
/// Bảng SIGNATURES: Chữ ký điện tử cho hợp đồng (Person 4 - Workflow & Signature)
/// </summary>
[Table("SIGNATURES")]
public class Signature
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public SignerType SignerType { get; set; }

    public Guid? InternalSignerId { get; set; }

    public Guid? PartnerSignerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string SignerNameSnapshot { get; set; } = string.Empty;

    [Required]
    public SignatureMethod SignatureMethod { get; set; }

    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(512)]
    public string SignatureHash { get; set; } = string.Empty;

    /// <summary>
    /// Kiểm tra tính hợp lệ của người ký theo ràng buộc SignerExclusive:
    /// Nếu SignerType = InternalUser -> InternalSignerId != null và PartnerSignerId == null
    /// Nếu SignerType = PartnerRepresentative -> PartnerSignerId != null và InternalSignerId == null
    /// </summary>
    public bool IsValid()
    {
        if (ContractId == Guid.Empty) return false;
        if (string.IsNullOrWhiteSpace(SignerNameSnapshot)) return false;
        if (string.IsNullOrWhiteSpace(SignatureHash)) return false;

        return SignerType switch
        {
            SignerType.InternalUser => InternalSignerId.HasValue && InternalSignerId.Value != Guid.Empty && PartnerSignerId == null,
            SignerType.PartnerRepresentative => PartnerSignerId.HasValue && PartnerSignerId.Value != Guid.Empty && InternalSignerId == null,
            _ => false
        };
    }
}
