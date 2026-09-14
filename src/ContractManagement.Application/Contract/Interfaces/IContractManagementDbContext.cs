using ContractManagement.Application.Workflow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contract.Interfaces;
public interface IContractManagementDbContext : IWorkflowDbContext
{
    DbSet<ContractManagement.Domain.Contract.Entities.ContractType> ContractTypes { get; }
    DbSet<ContractManagement.Domain.Contract.Entities.ContractTemplateVersion> ContractTemplateVersions { get; }
    DbSet<ContractManagement.Domain.Contract.Entities.Contract> Contracts { get; }
}
