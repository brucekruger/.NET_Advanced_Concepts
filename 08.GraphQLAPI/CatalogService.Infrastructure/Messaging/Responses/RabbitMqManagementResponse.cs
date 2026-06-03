namespace CatalogService.Infrastructure.Messaging.Responses;

/// <summary>
/// Response for connection status.
/// </summary>
public class ConnectionStatusResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection is open.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ hostname.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RabbitMQ port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the status check.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response containing queue information.
/// </summary>
public class QueueInfoResponse
{
    /// <summary>
    /// Gets or sets the queue information array.
    /// </summary>
    public QueueInfo[] Queues { get; set; } = [];

    /// <summary>
    /// Gets or sets the timestamp of the information retrieval.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Information about a specific queue.
/// </summary>
public class QueueInfo
{
    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of messages in the queue.
    /// </summary>
    public uint MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the number of active consumers.
    /// </summary>
    public uint ConsumerCount { get; set; }

    /// <summary>
    /// Gets or sets the queue type (main or dead-letter).
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Response for queue purge operation.
/// </summary>
public class PurgeResponse
{
    /// <summary>
    /// Gets or sets the queue name that was purged.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of messages removed.
    /// </summary>
    public uint MessagesRemoved { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the purge operation.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response containing exchange information.
/// </summary>
public class ExchangeInfoResponse
{
    /// <summary>
    /// Gets or sets the exchange information array.
    /// </summary>
    public ExchangeInfo[] Exchanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the timestamp of the information retrieval.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Information about a specific exchange.
/// </summary>
public class ExchangeInfo
{
    /// <summary>
    /// Gets or sets the exchange name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the exchange is durable.
    /// </summary>
    public bool Durable { get; set; }

    /// <summary>
    /// Gets or sets the routing key.
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;
}

/// <summary>
/// Error response for API failures.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error details.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}