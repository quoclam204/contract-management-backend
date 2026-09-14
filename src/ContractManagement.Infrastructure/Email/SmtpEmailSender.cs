using ContractManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace ContractManagement.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient email is required.", nameof(to));

        var host = _config["Email:Smtp:Host"];
        var portStr = _config["Email:Smtp:Port"];
        var user = _config["Email:Smtp:UserName"];
        var pass = _config["Email:Smtp:Password"];
        var from = _config["Email:Smtp:From"];
        var enableSslStr = _config["Email:Smtp:EnableSsl"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("SMTP not configured; skipping email to {To} subject {Subject}", to, subject);
            throw new InvalidOperationException("SMTP configuration is missing (Host/From).");
        }

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var p)) port = p;
        bool enableSsl = true;
        if (!string.IsNullOrWhiteSpace(enableSslStr) && bool.TryParse(enableSslStr, out var b)) enableSsl = b;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, pass ?? string.Empty);

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(to);

        _logger.LogInformation("Sending expiry email to {To} subject {Subject}", to, subject);
        await client.SendMailAsync(message, cancellationToken);
    }
}
