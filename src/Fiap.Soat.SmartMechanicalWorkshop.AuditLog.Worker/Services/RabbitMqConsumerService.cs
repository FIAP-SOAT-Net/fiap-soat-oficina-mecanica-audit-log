using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

public class RabbitMqConsumerService : IRabbitMqConsumerService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly RabbitMqConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        IOptions<RabbitMqConfiguration> config,
        ILogger<RabbitMqConsumerService> logger)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password
            };

            _logger.LogInformation("Connecting to RabbitMQ: {HostName}:{Port}",
                _config.HostName, _config.Port);

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _config.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue: _config.QueueName,
                exchange: _config.ExchangeName,
                routingKey: "#",
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(0, 1, false, cancellationToken);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to RabbitMQ");
            throw;
        }
    }

    public async Task StartConsumingAsync(
        Func<string, CancellationToken, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized. Call ConnectAsync first.");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Message received from RabbitMQ");

                await messageHandler(message, cancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);

                _logger.LogInformation("Message acknowledged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message, sending Nack");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _config.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Started consuming messages from queue: {QueueName}", _config.QueueName);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from RabbitMQ");

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
            _connection = null;
        }

        _logger.LogInformation("Disconnected from RabbitMQ");
    }
}
