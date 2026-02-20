using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Models;

public class AuditEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("data")]
    public string Data { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
