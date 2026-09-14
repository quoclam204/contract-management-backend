using ContractManagement.Domain.AI.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.AI.Interfaces;

public interface IAiDbContext
{
    DbSet<AiAnalysisResult> AiAnalysisResults { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
