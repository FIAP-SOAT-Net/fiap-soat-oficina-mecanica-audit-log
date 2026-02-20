using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRabbitMqConsumerService _rabbitMqConsumer;
    private readonly IEventProcessorService _eventProcessor;

    public Worker(
        ILogger<Worker> logger,
        IRabbitMqConsumerService rabbitMqConsumer,
        IEventProcessorService eventProcessor)
    {
        _logger = logger;
        _rabbitMqConsumer = rabbitMqConsumer;
        _eventProcessor = eventProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Audit Log Worker");

            await _rabbitMqConsumer.ConnectAsync(stoppingToken);

            await _rabbitMqConsumer.StartConsumingAsync(
                async (message, ct) => await _eventProcessor.ProcessEventAsync(message, ct),
                stoppingToken);

            _logger.LogInformation("Worker is now consuming messages from RabbitMQ");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Worker execution");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit Log Worker is stopping");

        await _rabbitMqConsumer.DisconnectAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
