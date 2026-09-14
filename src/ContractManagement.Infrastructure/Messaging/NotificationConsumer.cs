using ContractManagement.Application.Events.Identity;
using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ContractManagement.Infrastructure.Messaging;

/// <summary>
/// Minimal RabbitMQ consumer for <see cref="UserRegisteredEvent"/>.
/// Subscribes to <c>notification.events</c> queue bound to <c>notification_exchange</c>
/// with routing key <c>user.registered</c>.
/// Creates a welcome notification (SystemAnnouncement) for each new user.
/// </summary>
public sealed class NotificationConsumer : BackgroundService
{
    private const string ExchangeName = "notification_exchange";
    private const string QueueName = "notification.events";
    private const string RoutingKey = "user.registered";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationConsumer> _logger;

    private IModel? _channel;

    public NotificationConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationConsumer starting — subscribing to queue '{Queue}'", QueueName);

        _channel = _connectionFactory.CreateChannel();

        // Declare exchange (idempotent).
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare queue (idempotent).
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue → exchange with routing key.
        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

        // Use AsyncEventingBasicConsumer because factory.DispatchConsumersAsync = true
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            await HandleMessageAsync(ea);
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("NotificationConsumer is now consuming from queue '{Queue}'", QueueName);

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());

        // Deserialize.
        UserRegisteredEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<UserRegisteredEvent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize UserRegisteredEvent: {Json}", json);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        // Validate.
        if (evt is null || evt.UserId == Guid.Empty)
        {
            _logger.LogWarning("Malformed UserRegisteredEvent (null or empty UserId): {Json}", json);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        // Process: create a welcome notification via scoped INotificationService.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await notificationService.CreateAsync(new CreateNotificationRequest
            {
                UserId = evt.UserId,
                Type = NotificationType.SystemAnnouncement,
                ContractId = null
            });

            _logger.LogInformation("Created welcome notification for UserId {UserId}", evt.UserId);

            // ACK only after successful persistence.
            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create welcome notification for UserId {UserId}", evt.UserId);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        if (_channel is { IsOpen: true })
        {
            _channel.Close();
            _logger.LogInformation("NotificationConsumer channel closed");
        }
        _channel?.Dispose();
        base.Dispose();
    }
}
