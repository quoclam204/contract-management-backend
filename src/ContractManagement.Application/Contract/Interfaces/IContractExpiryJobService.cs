namespace ContractManagement.Application.Contract.Interfaces;

/// <summary>
/// Service interface for background scanning of contracts nearing expiration.
/// </summary>
public interface IContractExpiryJobService
{
    /// <summary>
    /// Scans contracts for upcoming expiration dates (30, 15, 7 days),
    /// transitions contract status from Active to Expiring when remaining days &lt;= 30,
    /// and creates notifications for contract owners.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing completion and count of processed/updated contracts.</returns>
    Task<int> ProcessContractExpirationsAsync(CancellationToken cancellationToken = default);
}
