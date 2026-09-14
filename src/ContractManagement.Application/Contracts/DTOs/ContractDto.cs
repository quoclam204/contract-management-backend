using ContractManagement.Domain.Contracts.Enums;

namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Data Transfer Object for Contract entity.
/// </summary>
public class ContractDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid ContractTypeId { get; set; }
    public string? ContractTypeName { get; set; }
    public Guid TemplateVersionUsedId { get; set; }
    public Guid PartnerId { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public ContractStatus Status { get; set; }
    public string? FileUrl { get; set; }
    public Guid? ParentContractId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
