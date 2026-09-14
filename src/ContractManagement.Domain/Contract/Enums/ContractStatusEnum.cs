namespace ContractManagement.Domain.Contract.Enums;

/// <summary>
/// Trạng thái hợp đồng: 0=Draft, 1=PendingApproval, 2=Approved, 3=Signed, 4=Active, 5=Expiring, 6=Renewed, 7=Terminated
/// Khớp với CHECK constraint CK_CONTRACTS_Status
/// </summary>
public enum ContractStatusEnum : byte
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Signed = 3,
    Active = 4,
    Expiring = 5,
    Renewed = 6,
    Terminated = 7
}
