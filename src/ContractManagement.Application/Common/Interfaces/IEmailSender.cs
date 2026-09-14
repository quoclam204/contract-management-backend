namespace ContractManagement.Application.Common.Interfaces;

/// <summary>
/// Minimal email abstraction for direct warnings (FR-08).
/// Infrastructure provides SmtpEmailSender; Application depends only on this interface.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
