namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Application abstraction for contract expiry tracking (FR-08).
/// No Hangfire types here — Hangfire scheduling lives in API/Infrastructure.
/// </summary>
public interface IContractExpiryJobService
{
    Task<int> ProcessContractExpirationsAsync(CancellationToken cancellationToken = default);
}
