using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Messaging.Events;
using CatalogService.Infrastructure.Messaging.Interfaces;

namespace CatalogService.Infrastructure.Messaging;

/// <summary>
/// Helper class for publishing product-related events.
/// </summary>
public class ProductEventPublisher
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductEventPublisher"/> class.
    /// </summary>
    public ProductEventPublisher(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
    }

    /// <summary>
    /// Publishes a product updated event.
    /// </summary>
    public async Task PublishProductUpdatedAsync(Product product, CancellationToken cancellationToken = default)
    {
        var productEvent = new ProductChangedEvent
        {
            ProductId = product.Id,
            ChangeType = ProductChangeType.Updated,
            Name = product.Name,
            Price = product.Price,
            Description = product.Description,
            Amount = product.Amount,
            Timestamp = DateTime.UtcNow
        };

        await _messagePublisher.PublishAsync(productEvent, cancellationToken);
    }

    /// <summary>
    /// Publishes a product created event.
    /// </summary>
    public async Task PublishProductCreatedAsync(Product product, CancellationToken cancellationToken = default)
    {
        var productEvent = new ProductChangedEvent
        {
            ProductId = product.Id,
            ChangeType = ProductChangeType.Created,
            Name = product.Name,
            Price = product.Price,
            Description = product.Description,
            Amount = product.Amount,
            Timestamp = DateTime.UtcNow
        };

        await _messagePublisher.PublishAsync(productEvent, cancellationToken);
    }

    /// <summary>
    /// Publishes a product deleted event.
    /// </summary>
    public async Task PublishProductDeletedAsync(int productId, CancellationToken cancellationToken = default)
    {
        var productEvent = new ProductChangedEvent
        {
            ProductId = productId,
            ChangeType = ProductChangeType.Deleted,
            Timestamp = DateTime.UtcNow
        };

        await _messagePublisher.PublishAsync(productEvent, cancellationToken);
    }
}