using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Configuration;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Repositories;
using Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MongoDbConfiguration>(builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddSingleton<IRabbitMqConsumerService, RabbitMqConsumerService>();
builder.Services.AddSingleton<IEventProcessorService, EventProcessorService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
