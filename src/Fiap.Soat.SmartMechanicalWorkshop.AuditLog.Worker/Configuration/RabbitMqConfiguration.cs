namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;

public class RabbitMqConfiguration
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ExchangeName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
