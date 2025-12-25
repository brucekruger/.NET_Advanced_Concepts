using CartService.Infrastructure.Messaging.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CartService.Infrastructure.Messaging;

/// <summary>
/// Background service for consuming messages from RabbitMQ.
/// </summary>
public class MessageConsumerHostedService : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer;
    private readonly ILogger<MessageConsumerHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConsumerHostedService"/> class.
    /// </summary>
    public MessageConsumerHostedService(
        IMessageConsumer messageConsumer,
        ILogger<MessageConsumerHostedService> logger)
    {
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Message Consumer Hosted Service is starting");
            await _messageConsumer.StartConsumeAsync(stoppingToken);
            
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message Consumer Hosted Service is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Message Consumer Hosted Service");
            throw;
        }
    }
}