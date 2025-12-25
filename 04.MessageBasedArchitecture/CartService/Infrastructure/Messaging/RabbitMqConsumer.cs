using CartService.Application.Interfaces;
using CartService.Infrastructure.Messaging.Configuration;
using CartService.Infrastructure.Messaging.Events;
using CartService.Infrastructure.Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CartService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the message consumer for CartService.
/// </summary>
public class RabbitMqConsumer : IMessageConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConsumer"/> class.
    /// </summary>
    public RabbitMqConsumer(IConnection connection,
        IServiceProvider serviceProvider,
        RabbitMqSettings settings,
        ILogger<RabbitMqConsumer> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = _connection.CreateChannelAsync().Result;
        InitializeQueueAsync().Wait();
    }

    /// <summary>
    /// Starts consuming messages from the queue.
    /// </summary>
    public async Task StartConsumeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += HandleMessageAsync;

            await _channel.BasicConsumeAsync(
                queue: _settings.ProductChangedQueueName,
                autoAck: false,
                consumerTag: "cart-service-consumer",
                consumer: consumer, cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ consumer started for queue: {QueueName}", 
                _settings.ProductChangedQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
            throw;
        }
    }

    /// <summary>
    /// Handles incoming messages from RabbitMQ.
    /// </summary>
    private async Task HandleMessageAsync(object? model, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var productChangedEvent = JsonSerializer.Deserialize<ProductChangedEvent>(json);

            if (productChangedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize product changed event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            await ProcessProductChangeAsync(productChangedEvent);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);

            _logger.LogInformation(
                "Message processed successfully. Product ID: {ProductId}, Correlation ID: {CorrelationId}",
                productChangedEvent.ProductId,
                productChangedEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message, sending to DLQ");
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    /// <summary>
    /// Processes a product change event by updating cart items.
    /// </summary>
    private Task ProcessProductChangeAsync(ProductChangedEvent productEvent)
    {
        // This is a placeholder implementation.
        // In a real scenario, you would:
        // 1. Retrieve all carts from the repository
        // 2. Find items matching the product ID
        // 3. Update the product information based on the change type
        // 4. Save the updated carts

        _logger.LogInformation(
            "Processing product change event - Product ID: {ProductId}, Change Type: {ChangeType}",
            productEvent.ProductId,
            productEvent.ChangeType);

        using (var scope = _serviceProvider.CreateScope())
        {
            var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

            // TODO: Implement actual cart update logic based on product changes

            // Use cartService to handle the message
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the RabbitMQ queue.
    /// </summary>
    private async Task InitializeQueueAsync()
    {
        try
        {
            // Queue initialization is handled by the publisher
            // But we ensure it exists from the consumer side too
            await _channel.QueueDeclarePassiveAsync(_settings.ProductChangedQueueName);
            _logger.LogInformation("RabbitMQ queue verified: {QueueName}", 
                _settings.ProductChangedQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ queue");
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