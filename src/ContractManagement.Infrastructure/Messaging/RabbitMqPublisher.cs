using System.Text;
using System.Text.Json;
using ContractManagement.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ContractManagement.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of <see cref="IMessagePublisher"/>.
/// Publishes messages as JSON to a specified exchange with a routing key.
/// Uses a transient channel per publish for thread-safety (IModel is not thread-safe).
/// </summary>
public class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public RabbitMqPublisher(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task PublishAsync<TMessage>(
        string exchange,
        string routingKey,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);

        cancellationToken.ThrowIfCancellationRequested();

        var messageType = typeof(TMessage).Name;

        try
        {
            using var channel = _connectionFactory.CreateChannel();

            // Ensure the exchange exists (idempotent declaration)
            channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.DeliveryMode = 2; // Persistent
            properties.MessageId = Guid.NewGuid().ToString("N");
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = messageType;

            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug(
                "Published {MessageType} to exchange '{Exchange}' with routing key '{RoutingKey}' (MessageId: {MessageId})",
                messageType, exchange, routingKey, properties.MessageId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Failed to publish {MessageType} to exchange '{Exchange}' with routing key '{RoutingKey}'",
                messageType, exchange, routingKey);
            throw;
        }

        return Task.CompletedTask;
    }
}
