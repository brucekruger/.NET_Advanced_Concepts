namespace CartService.Infrastructure.Messaging.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ connection and behavior.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// Gets or sets the hostname of the RabbitMQ server.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host path.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for message delivery.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in milliseconds between retry attempts.
    /// </summary>
    public int RetryDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the queue name for products updates.
    /// </summary>
    public string ProductChangedQueueName { get; set; } = "products.changed";

    /// <summary>
    /// Gets or sets the exchange name for product events.
    /// </summary>
    public string ProductExchangeName { get; set; } = "catalog.products";

    /// <summary>
    /// Gets or sets the routing key for product events.
    /// </summary>
    public string ProductRoutingKey { get; set; } = "product.changed";

    /// <summary>
    /// Gets or sets the dead letter queue name for failed messages.
    /// </summary>
    public string DeadLetterQueueName { get; set; } = "products.changed.dlq";

    /// <summary>
    /// Gets or sets the dead letter exchange name.
    /// </summary>
    public string DeadLetterExchangeName { get; set; } = "catalog.products.dlx";
}