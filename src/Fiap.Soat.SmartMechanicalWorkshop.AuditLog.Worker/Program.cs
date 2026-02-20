using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MongoDbConfiguration>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
