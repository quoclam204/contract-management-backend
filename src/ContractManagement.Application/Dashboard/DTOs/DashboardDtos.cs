namespace ContractManagement.Application.Dashboard.DTOs;

/// <summary>
/// Summary KPI stats for dashboard
/// </summary>
public class DashboardSummaryDto
{
    public int TotalContracts { get; set; }
    public decimal TotalValue { get; set; }
    public int ActiveContractsCount { get; set; }
    public decimal ActiveContractsValue { get; set; }
    public int ExpiringContractsCount { get; set; }
    public decimal ExpiringContractsValue { get; set; }
    public int PendingApprovalCount { get; set; }
    public decimal PendingApprovalValue { get; set; }
    public int DraftCount { get; set; }
    public int SignedCount { get; set; }
    public int TerminatedCount { get; set; }
}

/// <summary>
/// Contract statistics aggregated by status
/// </summary>
public class ContractStatusSummaryDto
{
    public byte Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Contract statistics aggregated by department
/// </summary>
public class DepartmentContractSummaryDto
{
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Contract statistics aggregated by partner
/// </summary>
public class PartnerContractSummaryDto
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Contract statistics aggregated by creation month and year
/// </summary>
public class MonthlyContractSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}
