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
        int retryCount = 0;
        const int maxRetries = 30; // Try for 5 minutes (30 * 10 seconds)
        const int retryDelayMs = 10000; // 10 seconds between retries

        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("Message Consumer Hosted Service is starting (attempt {Attempt}/{MaxRetries})",
                    retryCount + 1, maxRetries);
                await _messageConsumer.StartConsumeAsync(stoppingToken);

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
                return; // Exit if successful
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message Consumer Hosted Service is shutting down");
                return;
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
            {
                // Queue doesn't exist yet - retry later
                retryCount++;
                if (retryCount < maxRetries)
                {
                    _logger.LogWarning(
                        "Product queue not available yet, retrying in {DelaySeconds} seconds (attempt {Attempt}/{MaxRetries})",
                        retryDelayMs / 1000, retryCount, maxRetries);
                    await Task.Delay(retryDelayMs, stoppingToken);
                }
                else
                {
                    _logger.LogError("Failed to start consuming messages after {MaxRetries} retries", maxRetries);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Message Consumer Hosted Service");
                throw;
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("Message Consumer Hosted Service exceeded maximum retry attempts");
        }
    }
}
