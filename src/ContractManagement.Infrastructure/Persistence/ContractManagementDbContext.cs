using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence;

public class ContractManagementDbContext : DbContext, IWorkflowDbContext, IContractDbContext
{
    public ContractManagementDbContext(DbContextOptions<ContractManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<ContractTemplateVersion> ContractTemplateVersions => Set<ContractTemplateVersion>();

    public async Task<Guid> GetDefaultApproverIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await Database.SqlQueryRaw<Guid>("SELECT TOP 1 Id AS [Value] FROM dbo.USERS").FirstOrDefaultAsync(cancellationToken);
            if (user != Guid.Empty)
                return user;
        }
        catch
        {
            // Table USERS might not exist yet before migration
        }

        return Guid.NewGuid();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations from assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractManagementDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}