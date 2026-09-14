using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Common.Interfaces
{
    public interface IAttachmentDbContext
    {
        DbSet<Attachment> Attachments { get; }
        DbSet<ContractManagement.Domain.Contract.Entities.Contract> Contracts { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
