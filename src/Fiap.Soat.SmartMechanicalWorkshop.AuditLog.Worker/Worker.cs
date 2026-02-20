using System.Text;
using System.Text.Json;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqConfiguration _rabbitMqConfig;
    private readonly IMongoCollection<AuditEvent> _mongoCollection;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqConfiguration> rabbitMqConfig,
        IOptions<MongoDbConfiguration> mongoDbConfig)
    {
        _logger = logger;
        _rabbitMqConfig = rabbitMqConfig.Value;

        var mongoClient = new MongoClient(mongoDbConfig.Value.GetConnectionStringWithCredentials());
        var database = mongoClient.GetDatabase(mongoDbConfig.Value.DatabaseName);
        _mongoCollection = database.GetCollection<AuditEvent>(mongoDbConfig.Value.CollectionName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Audit Log Worker");
            await ConnectToRabbitMqAsync(stoppingToken);
            if (_channel is null)
            {
                throw new InvalidOperationException("RabbitMQ channel is not initialized");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Received message: {Message}", message);
                    await ProcessAndSaveEventAsync(message, stoppingToken);
                    await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
                    _logger.LogInformation("Event processed and acknowledged successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message, sending Nack");
                    await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: _rabbitMqConfig.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Worker is now consuming messages from RabbitMQ");

            while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Worker execution");
            throw;
        }
    }

    private async Task ConnectToRabbitMqAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqConfig.HostName,
            Port = _rabbitMqConfig.Port,
            UserName = _rabbitMqConfig.UserName,
            Password = _rabbitMqConfig.Password
        };

        _logger.LogInformation("Connecting to RabbitMQ: {HostName}:{Port}",
            _rabbitMqConfig.HostName, _rabbitMqConfig.Port);

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _rabbitMqConfig.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqConfig.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: _rabbitMqConfig.QueueName,
            exchange: _rabbitMqConfig.ExchangeName,
            routingKey: "#",
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(0, 1, false, cancellationToken);

        _logger.LogInformation("RabbitMQ connection established and channel configured successfully");
    }

    private async Task ProcessAndSaveEventAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
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
                    ? data.GetString() ?? message
                    : message,
                Source = root.TryGetProperty("Source", out var source)
                    ? source.GetString() ?? "RabbitMQ"
                    : "RabbitMQ",
                ReceivedAt = DateTime.UtcNow
            };

            await _mongoCollection.InsertOneAsync(auditEvent, cancellationToken: cancellationToken);
            _logger.LogInformation("Event {Event} saved successfully", auditEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing and saving event");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit Log Worker is stopping");

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}