using ContractManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence;

/// <summary>
/// Foundation DbContext for Contract Management System.
/// To be extended by developers with actual DbSets for each module.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added here by developers for each module
    // Example:
    // public DbSet<Contract> Contracts { get; set; }
    // public DbSet<WorkflowInstance> WorkflowInstances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Module-specific configurations will be added here
        // Example:
        // modelBuilder.Entity<Contract>(entity =>
        // {
        //     entity.ToTable("Contracts");
        //     entity.HasKey(e => e.Id);
        //     // ... more configuration
        // });
    }
}