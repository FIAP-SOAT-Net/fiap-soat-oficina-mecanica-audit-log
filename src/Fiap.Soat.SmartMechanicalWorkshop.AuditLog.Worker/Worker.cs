using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IRabbitMqConsumerService rabbitMqConsumer,
    IEventProcessorService eventProcessor)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Audit Log Worker");

            await rabbitMqConsumer.ConnectAsync(stoppingToken);

            await rabbitMqConsumer.StartConsumingAsync(
                async (message, ct) => await eventProcessor.ProcessEventAsync(message, ct),
                stoppingToken);

            logger.LogInformation("Worker is now consuming messages from RabbitMQ");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in Worker execution");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Audit Log Worker is stopping");

        await rabbitMqConsumer.DisconnectAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
