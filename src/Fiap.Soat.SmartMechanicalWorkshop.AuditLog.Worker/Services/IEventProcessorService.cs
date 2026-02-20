namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

public interface IEventProcessorService
{
    Task ProcessEventAsync(string message, CancellationToken cancellationToken = default);
}
