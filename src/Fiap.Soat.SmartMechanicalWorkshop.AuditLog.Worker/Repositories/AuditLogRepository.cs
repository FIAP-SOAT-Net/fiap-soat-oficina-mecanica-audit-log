using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditEvent> _collection;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(
        IOptions<MongoDbConfiguration> mongoDbConfig,
        ILogger<AuditLogRepository> logger)
    {
        _logger = logger;

        var mongoClient = new MongoClient(mongoDbConfig.Value.GetConnectionStringWithCredentials());
        var database = mongoClient.GetDatabase(mongoDbConfig.Value.DatabaseName);
        _collection = database.GetCollection<AuditEvent>(mongoDbConfig.Value.CollectionName);

        _logger.LogInformation("MongoDB repository initialized for database: {DatabaseName}",
            mongoDbConfig.Value.DatabaseName);
    }

    public async Task SaveEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(auditEvent, cancellationToken: cancellationToken);
            _logger.LogInformation("Event {EventType} saved to MongoDB successfully", auditEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving event {EventType} to MongoDB", auditEvent.EventType);
            throw;
        }
    }
}
