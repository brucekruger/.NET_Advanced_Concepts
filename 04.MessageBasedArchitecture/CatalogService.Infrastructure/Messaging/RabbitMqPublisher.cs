using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using CatalogService.Infrastructure.Messaging.Configuration;
using CatalogService.Infrastructure.Messaging.Interfaces;

namespace CatalogService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the message publisher.
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqPublisher"/> class.
    /// </summary>
    public RabbitMqPublisher(
        IConnection connection,
        RabbitMqSettings settings,
        ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = _connection.CreateChannelAsync().Result;
        InitializeExchangeAsync().Wait();
    }

    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    public async Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        try
        {
            var json = JsonSerializer.Serialize(eventMessage);
            var body = Encoding.UTF8.GetBytes(json);
                
            var basicProperties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = MediaTypeNames.Application.Json
            };

            await _channel.BasicPublishAsync(
                exchange: _settings.ProductExchangeName,
                routingKey: _settings.ProductRoutingKey,
                mandatory: false,
                basicProperties: basicProperties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Message of type {MessageType} published successfully with correlation ID: {CorrelationId}",
                typeof(T).Name,
                eventMessage.GetType().GetProperty("CorrelationId")?.GetValue(eventMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Initializes the RabbitMQ exchange and queue.
    /// </summary>
    private async Task InitializeExchangeAsync()
    {
        try
        {
            // Declare the main exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.ProductExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare the dead letter exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.DeadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare the dead letter queue
            await _channel.QueueDeclareAsync(
                queue: _settings.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind dead letter queue to dead letter exchange
            await _channel.QueueBindAsync(
                queue: _settings.DeadLetterQueueName,
                exchange: _settings.DeadLetterExchangeName,
                routingKey: _settings.ProductRoutingKey);

            // Declare the main queue with dead letter exchange configuration
            await _channel.QueueDeclareAsync(
                queue: _settings.ProductChangedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _settings.DeadLetterExchangeName },
                    { "x-dead-letter-routing-key", _settings.ProductRoutingKey }
                });

            // Bind the queue to the exchange
            await _channel.QueueBindAsync(
                queue: _settings.ProductChangedQueueName,
                exchange: _settings.ProductExchangeName,
                routingKey: _settings.ProductRoutingKey);

            _logger.LogInformation("RabbitMQ exchange and queue initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ exchange and queue");
            throw;
        }
    }

    /// <summary>
    /// Disposes the RabbitMQ resources.
    /// </summary>
    public void Dispose()
    {
        _channel.Dispose();
        GC.SuppressFinalize(this);
    }
}