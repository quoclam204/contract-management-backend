using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Interface for database context operations for contract entities.
/// </summary>
public interface IContractDbContext
{
    DbSet<ContractManagement.Domain.Contracts.Entities.Contract> Contracts { get; }
    DbSet<ContractManagement.Domain.Contracts.Entities.ContractType> ContractTypes { get; }
    DbSet<ContractManagement.Domain.Contracts.Entities.ContractTemplateVersion> ContractTemplateVersions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}