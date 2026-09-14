using ContractManagement.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Identity.Interfaces;

public interface IIdentityDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
