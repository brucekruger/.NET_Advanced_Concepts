namespace CartService.Infrastructure.Messaging.Interfaces;

/// <summary>
/// Interface for consuming messages from a message broker.
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consuming messages from the message broker.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartConsumeAsync(CancellationToken cancellationToken = default);
}