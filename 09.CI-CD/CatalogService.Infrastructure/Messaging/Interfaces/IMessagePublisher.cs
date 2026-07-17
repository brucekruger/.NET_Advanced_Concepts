namespace CatalogService.Infrastructure.Messaging.Interfaces;

/// <summary>
/// Interface for publishing messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="eventMessage">The event message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class;
}