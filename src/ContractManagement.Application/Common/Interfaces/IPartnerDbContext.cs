using ContractManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Common.Interfaces
{
    public interface IPartnerDbContext
    {
        DbSet<Partner> Partners { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}