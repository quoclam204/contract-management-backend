using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Workflow.Interfaces;

public interface IWorkflowDbContext
{
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<Signature> Signatures { get; }
    Task<Guid> GetDefaultApproverIdAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
