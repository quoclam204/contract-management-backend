using ContractManagement.Application.Dashboard.DTOs;

namespace ContractManagement.Application.Dashboard.Interfaces;

/// <summary>
/// Service interface for retrieving Contract Dashboard and Report statistics.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns overall summary KPI metrics.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns contract counts and total values grouped by status (0 to 7).
    /// </summary>
    Task<List<ContractStatusSummaryDto>> GetByStatusAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns contract counts and total values grouped by department.
    /// </summary>
    Task<List<DepartmentContractSummaryDto>> GetByDepartmentAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns contract counts and total values grouped by partner.
    /// </summary>
    Task<List<PartnerContractSummaryDto>> GetByPartnerAsync(int top = 10, Guid? userIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns monthly contract statistics grouped by creation year and month.
    /// </summary>
    Task<List<MonthlyContractSummaryDto>> GetByTimeAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default);
}
