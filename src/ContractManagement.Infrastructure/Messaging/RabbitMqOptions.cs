namespace ContractManagement.Infrastructure.Messaging;

/// <summary>
/// Configuration options for RabbitMQ connection.
/// Binds to the "RabbitMq" configuration section.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ host name or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default 5672).
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host (default "/").
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Client-provided connection name for identification in RabbitMQ management UI.
    /// </summary>
    public string ConnectionName { get; set; } = "ContractManagement";

    /// <summary>
    /// Whether automatic recovery is enabled (default true).
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval (default 5 seconds).
    /// </summary>
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Creates the RabbitMQ.Client ConnectionFactory from these options.
    /// </summary>
    public RabbitMQ.Client.ConnectionFactory CreateConnectionFactory()
    {
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = Host,
            Port = Port,
            VirtualHost = VirtualHost,
            UserName = Username,
            Password = Password,
            ClientProvidedName = ConnectionName,
            AutomaticRecoveryEnabled = AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = NetworkRecoveryInterval,
        };
        return factory;
    }
}