using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Dashboard.DTOs;
using ContractManagement.Application.Dashboard.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Dashboard.Services;

/// <summary>
/// Service implementing dashboard statistics and basic reporting queries.
/// Uses current Contracts module (IContractDbContext / Domain.Contracts.Entities.Contract) -> dbo.CONTRACTS.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IContractDbContext _contractDbContext;
    private readonly IIdentityDbContext _identityDbContext;
    private readonly IPartnerDbContext _partnerDbContext;

    public DashboardService(
        IContractDbContext contractDbContext,
        IIdentityDbContext identityDbContext,
        IPartnerDbContext partnerDbContext)
    {
        _contractDbContext = contractDbContext;
        _identityDbContext = identityDbContext;
        _partnerDbContext = partnerDbContext;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default)
    {
        var query = FilterByOwner(_contractDbContext.Contracts.AsNoTracking(), userIdFilter);

        var now = DateTime.UtcNow;
        var expiringDeadline = now.AddDays(30);

        // SQL-side aggregations (no full materialization)
        var totalContracts = await query.CountAsync(cancellationToken);
        var totalValue = await query.SumAsync(c => c.Value, cancellationToken);

        var activeCount = await query.CountAsync(c => c.Status == ContractStatus.Active, cancellationToken);
        var activeValue = await query.Where(c => c.Status == ContractStatus.Active).SumAsync(c => c.Value, cancellationToken);

        var pendingCount = await query.CountAsync(c => c.Status == ContractStatus.PendingApproval, cancellationToken);
        var pendingValue = await query.Where(c => c.Status == ContractStatus.PendingApproval).SumAsync(c => c.Value, cancellationToken);

        // MVP KPI "Sắp Hết Hạn": Active + ExpiryDate within [now, now+30d]
        var expiringCount = await query.CountAsync(
            c => c.Status == ContractStatus.Active
                 && c.ExpiryDate >= now
                 && c.ExpiryDate <= expiringDeadline,
            cancellationToken);
        var expiringValue = await query
            .Where(c => c.Status == ContractStatus.Active && c.ExpiryDate >= now && c.ExpiryDate <= expiringDeadline)
            .SumAsync(c => c.Value, cancellationToken);

        var draftCount = await query.CountAsync(c => c.Status == ContractStatus.Draft, cancellationToken);
        var signedCount = await query.CountAsync(c => c.Status == ContractStatus.Signed, cancellationToken);
        var terminatedCount = await query.CountAsync(c => c.Status == ContractStatus.Terminated, cancellationToken);

        return new DashboardSummaryDto
        {
            TotalContracts = totalContracts,
            TotalValue = totalValue,
            ActiveContractsCount = activeCount,
            ActiveContractsValue = activeValue,
            ExpiringContractsCount = expiringCount,
            ExpiringContractsValue = expiringValue,
            PendingApprovalCount = pendingCount,
            PendingApprovalValue = pendingValue,
            DraftCount = draftCount,
            SignedCount = signedCount,
            TerminatedCount = terminatedCount
        };
    }

    public async Task<List<ContractStatusSummaryDto>> GetByStatusAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default)
    {
        var query = FilterByOwner(_contractDbContext.Contracts.AsNoTracking(), userIdFilter);

        var statusGroups = await query
            .GroupBy(c => c.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(c => c.Value)
            })
            .ToListAsync(cancellationToken);

        var resultDict = statusGroups.ToDictionary(g => (byte)g.Status);

        var result = new List<ContractStatusSummaryDto>();
        foreach (ContractStatus statusEnum in Enum.GetValues(typeof(ContractStatus)))
        {
            byte statusByte = (byte)statusEnum;
            if (resultDict.TryGetValue(statusByte, out var group))
            {
                result.Add(new ContractStatusSummaryDto
                {
                    Status = statusByte,
                    StatusName = statusEnum.ToString(),
                    Count = group.Count,
                    TotalValue = group.TotalValue
                });
            }
            else
            {
                result.Add(new ContractStatusSummaryDto
                {
                    Status = statusByte,
                    StatusName = statusEnum.ToString(),
                    Count = 0,
                    TotalValue = 0m
                });
            }
        }

        return result;
    }

    public async Task<List<DepartmentContractSummaryDto>> GetByDepartmentAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default)
    {
        var query = FilterByOwner(_contractDbContext.Contracts.AsNoTracking(), userIdFilter);

        var deptGroups = await (
            from c in query
            join u in _identityDbContext.Users.AsNoTracking() on c.OwnerId equals u.Id into uGroup
            from user in uGroup.DefaultIfEmpty()
            join d in _identityDbContext.Departments.AsNoTracking() on user.DepartmentId equals d.Id into dGroup
            from dept in dGroup.DefaultIfEmpty()
            group c by new
            {
                DepartmentId = (Guid?)user.DepartmentId,
                DepartmentName = dept != null ? dept.Name : "Unassigned"
            } into g
            select new DepartmentContractSummaryDto
            {
                DepartmentId = g.Key.DepartmentId,
                DepartmentName = g.Key.DepartmentName,
                Count = g.Count(),
                TotalValue = g.Sum(c => c.Value)
            }
        ).ToListAsync(cancellationToken);

        return deptGroups.OrderByDescending(d => d.TotalValue).ThenByDescending(d => d.Count).ToList();
    }

    public async Task<List<PartnerContractSummaryDto>> GetByPartnerAsync(int top = 10, Guid? userIdFilter = null, CancellationToken cancellationToken = default)
    {
        var query = FilterByOwner(_contractDbContext.Contracts.AsNoTracking(), userIdFilter);

        var partnerGroupsQuery =
            from c in query
            join p in _partnerDbContext.Partners.AsNoTracking() on c.PartnerId equals p.Id into pGroup
            from partner in pGroup.DefaultIfEmpty()
            group c by new
            {
                c.PartnerId,
                PartnerName = partner != null ? partner.Name : "Unknown Partner"
            } into g
            select new PartnerContractSummaryDto
            {
                PartnerId = g.Key.PartnerId,
                PartnerName = g.Key.PartnerName,
                Count = g.Count(),
                TotalValue = g.Sum(c => c.Value)
            };

        var sortedQuery = partnerGroupsQuery
            .OrderByDescending(p => p.TotalValue)
            .ThenByDescending(p => p.Count);

        if (top > 0)
        {
            return await sortedQuery.Take(top).ToListAsync(cancellationToken);
        }

        return await sortedQuery.ToListAsync(cancellationToken);
    }

    public async Task<List<MonthlyContractSummaryDto>> GetByTimeAsync(Guid? userIdFilter = null, CancellationToken cancellationToken = default)
    {
        var query = FilterByOwner(_contractDbContext.Contracts.AsNoTracking(), userIdFilter);

        var timeGroups = await query
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new MonthlyContractSummaryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count(),
                TotalValue = g.Sum(c => c.Value)
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ToListAsync(cancellationToken);

        return timeGroups;
    }

    private static IQueryable<ContractManagement.Domain.Contracts.Entities.Contract> FilterByOwner(IQueryable<ContractManagement.Domain.Contracts.Entities.Contract> query, Guid? userIdFilter)
    {
        if (userIdFilter.HasValue && userIdFilter.Value != Guid.Empty)
        {
            return query.Where(c => c.OwnerId == userIdFilter.Value);
        }
        return query;
    }
}
