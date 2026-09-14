using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain;
using ContractManagement.Domain.Contract.Entities;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Workflow.Entities;
using DomainNotification = ContractManagement.Domain.Notification.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence;

public class ContractManagementDbContext : DbContext, 
    IContractManagementDbContext, 
    IWorkflowDbContext, 
    IIdentityDbContext, 
    IPartnerDbContext, 
    INotificationDbContext,
    IAttachmentDbContext
{
    public ContractManagementDbContext(DbContextOptions<ContractManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<ContractTemplateVersion> ContractTemplateVersions => Set<ContractTemplateVersion>();
    public DbSet<Contract> Contracts => Set<Contract>();
    
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<DomainNotification.Notification> Notifications => Set<DomainNotification.Notification>();

    public async Task<Guid> GetDefaultApproverIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await Database.SqlQueryRaw<Guid>("SELECT TOP 1 Id FROM dbo.USERS").FirstOrDefaultAsync(cancellationToken);
            if (user != Guid.Empty)
                return user;
        }
        // Table USERS might not exist yet before migration
        catch
        {
            // Return a new Guid if there's an error (table doesn't exist yet)
            return Guid.NewGuid();
        }
        
        // Default return if no user found
        return Guid.NewGuid();
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations from assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractManagementDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
