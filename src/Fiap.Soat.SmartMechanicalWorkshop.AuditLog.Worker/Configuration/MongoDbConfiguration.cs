namespace Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;

public class MongoDbConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public string GetConnectionStringWithCredentials()
    {
        if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
        {
            return ConnectionString;
        }
        var uri = new Uri(ConnectionString);
        return $"mongodb://{UserName}:{Password}@{uri.Host}:{uri.Port}";
    }
}
