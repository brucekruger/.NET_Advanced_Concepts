namespace CatalogService.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when a product is changed in the catalog.
/// </summary>
public class ProductChangedEvent
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the type of change (Created, Updated, Deleted).
    /// </summary>
    public ProductChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the available amount of the product.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the correlation ID for tracing purposes.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Enumeration of product change types.
/// </summary>
public enum ProductChangeType
{
    /// <summary>
    /// Product was created.
    /// </summary>
    Created,

    /// <summary>
    /// Product was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// Product was deleted.
    /// </summary>
    Deleted
}