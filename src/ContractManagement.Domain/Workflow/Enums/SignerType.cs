namespace ContractManagement.Domain.Workflow.Enums;

/// <summary>
/// Loại người ký: 0 = InternalUser (Người dùng nội bộ), 1 = PartnerRepresentative (Đại diện đối tác)
/// </summary>
public enum SignerType : byte
{
    InternalUser = 0,
    PartnerRepresentative = 1
}
