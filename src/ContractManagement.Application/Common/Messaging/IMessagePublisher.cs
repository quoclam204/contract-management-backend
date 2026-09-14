using System.Threading.Tasks;

namespace ContractManagement.Application.Common.Messaging;

/// <summary>
/// Application-level port for publishing messages to a message broker.
/// Implementations belong in the Infrastructure layer.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the specified exchange with the given routing key.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message payload.</typeparam>
    /// <param name="exchange">The exchange name to publish to.</param>
    /// <param name="routingKey">The routing key for message routing.</param>
    /// <param name="message">The message payload to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}