using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Dashboard.DTOs;
using ContractManagement.Application.Dashboard.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers;

/// <summary>
/// REST API Controller for Contract Management Dashboard & Basic Reports (FR-12).
/// Provides KPI summaries, status breakdowns, department statistics, partner totals, and monthly creation trends.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// <summary>
    /// Gets overall summary KPI metrics (total contracts, total value, active, expiring, pending approval, etc.).
    /// </summary>
    [HttpGet("summary")]
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var filter = DetermineUserIdFilter(userId);
        var result = await _dashboardService.GetSummaryAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets contract count and total value aggregated by status (0 to 7).
    /// </summary>
    [HttpGet("by-status")]
    [HttpGet("status-distribution")]
    public async Task<ActionResult<List<ContractStatusSummaryDto>>> GetByStatus([FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var filter = DetermineUserIdFilter(userId);
        var result = await _dashboardService.GetByStatusAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets contract count and total value aggregated by department.
    /// </summary>
    [HttpGet("by-department")]
    [HttpGet("value-by-type")]
    public async Task<ActionResult<List<DepartmentContractSummaryDto>>> GetByDepartment([FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var filter = DetermineUserIdFilter(userId);
        var result = await _dashboardService.GetByDepartmentAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets contract count and total value aggregated by partner (top N partners).
    /// </summary>
    [HttpGet("by-partner")]
    public async Task<ActionResult<List<PartnerContractSummaryDto>>> GetByPartner(
        [FromQuery] int top = 10,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var filter = DetermineUserIdFilter(userId);
        var result = await _dashboardService.GetByPartnerAsync(top, filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets contract count and total value aggregated by creation month and year.
    /// </summary>
    [HttpGet("by-time")]
    [HttpGet("contract-trends")]
    public async Task<ActionResult<List<MonthlyContractSummaryDto>>> GetByTime([FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var filter = DetermineUserIdFilter(userId);
        var result = await _dashboardService.GetByTimeAsync(filter, cancellationToken);
        return Ok(result);
    }

    private Guid? DetermineUserIdFilter(Guid? queryUserId)
    {
        var currentRole = _currentUserService.Role;
        bool isAdminOrManager = !string.IsNullOrEmpty(currentRole) &&
            (currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
             currentRole.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
             currentRole.Equals("0", StringComparison.OrdinalIgnoreCase) ||
             currentRole.Equals("1", StringComparison.OrdinalIgnoreCase));

        if (isAdminOrManager)
        {
            return queryUserId;
        }

        return _currentUserService.UserId;
    }
}
