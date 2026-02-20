using System.Text.Json;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Models;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Repositories;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

public class EventProcessorService : IEventProcessorService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<EventProcessorService> _logger;

    public EventProcessorService(
        IAuditLogRepository repository,
        ILogger<EventProcessorService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ProcessEventAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing event message");

            using var jsonDocument = JsonDocument.Parse(message);
            var root = jsonDocument.RootElement;
            const string unknown = "Unknown";

            var auditEvent = new AuditEvent
            {
                EventType = root.TryGetProperty("EventType", out var eventType)
                    ? eventType.GetString() ?? unknown
                    : unknown,
                EntityType = root.TryGetProperty("EntityType", out var entityType)
                    ? entityType.GetString() ?? unknown
                    : unknown,
                Timestamp = root.TryGetProperty("Timestamp", out var timestamp)
                    ? timestamp.GetDateTime()
                    : DateTime.UtcNow,
                Data = root.TryGetProperty("Data", out var data)
                    ? data.GetRawText() ?? message
                    : message,
                Source = root.TryGetProperty("Source", out var source)
                    ? source.GetString() ?? "RabbitMQ"
                    : "RabbitMQ",
                ReceivedAt = DateTime.UtcNow
            };

            await _repository.SaveEventAsync(auditEvent, cancellationToken);

            _logger.LogInformation("Event processed successfully: {EventType}", auditEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event");
            throw;
        }
    }
}
