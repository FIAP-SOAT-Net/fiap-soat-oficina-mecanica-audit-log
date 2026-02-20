using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Models;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Repositories;

public interface IAuditLogRepository
{
    Task SaveEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
