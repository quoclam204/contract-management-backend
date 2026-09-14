using ContractManagement.Application.Workflow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contract.Interfaces;
public interface IContractManagementDbContext : IWorkflowDbContext
{
    DbSet<ContractManagement.Domain.Contracts.Entities.ContractType> ContractTypes { get; }
    DbSet<ContractManagement.Domain.Contracts.Entities.ContractTemplateVersion> ContractTemplateVersions { get; }
    DbSet<ContractManagement.Domain.Contracts.Entities.Contract> Contracts { get; }
}
