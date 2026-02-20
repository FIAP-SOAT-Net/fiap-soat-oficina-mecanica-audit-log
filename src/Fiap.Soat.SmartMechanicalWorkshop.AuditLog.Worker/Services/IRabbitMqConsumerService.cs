namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

public interface IRabbitMqConsumerService
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task StartConsumingAsync(Func<string, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
