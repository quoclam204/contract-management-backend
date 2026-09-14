using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ContractManagement.Infrastructure.Messaging;

/// <summary>
/// Manages a shared, long-lived RabbitMQ connection.
/// Registered as a Singleton and disposed at application shutdown.
/// The underlying ConnectionFactory has automatic recovery enabled by default,
/// so transient network failures are handled transparently.
/// </summary>
public sealed class RabbitMqConnectionFactory : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly object _lock = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the shared connection, creating it lazily on first use.
    /// Thread-safe via double-checked locking.
    /// </summary>
    public IConnection GetConnection()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqConnectionFactory));

        if (_connection is { IsOpen: true })
            return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation(
                "Creating RabbitMQ connection to {Host}:{Port}/{VirtualHost} as '{ConnectionName}'",
                _options.Host, _options.Port, _options.VirtualHost, _options.ConnectionName);

            var factory = _options.CreateConnectionFactory();

            // DispatchConsumersAsync enables async consumer support (RabbitMQ.Client 6.x)
            factory.DispatchConsumersAsync = true;

            _connection = factory.CreateConnection();

            _logger.LogInformation("RabbitMQ connection established successfully");
        }

        return _connection;
    }

    /// <summary>
    /// Creates a new channel (IModel) from the shared connection.
    /// Callers are responsible for disposing the returned channel.
    /// </summary>
    public IModel CreateChannel()
    {
        return GetConnection().CreateModel();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_connection is { IsOpen: true })
            {
                _connection.Close();
                _logger.LogInformation("RabbitMQ connection closed");
            }

            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection");
        }
    }
}
