using ContractManagement.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Interface for database context operations for contract entities.
/// </summary>
public interface IContractDbContext
{
    DbSet<Contract> Contracts { get; }
    DbSet<ContractType> ContractTypes { get; }
    DbSet<ContractTemplateVersion> ContractTemplateVersions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}