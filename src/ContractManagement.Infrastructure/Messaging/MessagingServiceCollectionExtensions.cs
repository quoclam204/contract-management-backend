using ContractManagement.Application.Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContractManagement.Infrastructure.Messaging;

/// <summary>
/// Extension methods for registering RabbitMQ messaging services in the DI container.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers RabbitMQ messaging infrastructure:
    /// <list type="bullet">
    ///   <item><see cref="RabbitMqOptions"/> bound from the "RabbitMq" configuration section</item>
    ///   <item><see cref="RabbitMqConnectionFactory"/> as Singleton (shared connection)</item>
    ///   <item><see cref="RabbitMqPublisher"/> as Singleton implementing <see cref="IMessagePublisher"/></item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration from "RabbitMq" section
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        // Connection factory — singleton, manages a single shared connection
        services.AddSingleton<RabbitMqConnectionFactory>();

        // Publisher — singleton, creates transient channels per publish
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        // NotificationConsumer — hosted service consuming RabbitMQ messages
        services.AddHostedService<NotificationConsumer>();

        return services;
    }
}
