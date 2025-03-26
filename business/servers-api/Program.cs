using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RabbitMQ.Client;
using Serilog;
using servers_api.api.rest.minimal.common;
using servers_api.api.rest.minimal.http;
using servers_api.middleware;
using servers_api.models.configurationsettings;
using servers_api.models.entities;
using servers_api.models.outbox;
using servers_api.repositories;
using servers_api.services.brokers.bpmintegration;

Console.Title = "integration api";

var builder = WebApplication.CreateBuilder(args);
var cts = new CancellationTokenSource();

// Настройка логирования
builder.Host.UseSerilog((ctx, cfg) =>
{
cfg.MinimumLevel.Information()
   .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
   .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
   .Enrich.FromLogContext();
});

try
{
	var services = builder.Services;
	var configuration = builder.Configuration;

	services.AddControllers();

	services.AddCommonServices();
	services.AddHttpServices();
	services.AddFactoryServices();
	services.AddApiServices();
	services.AddRabbitMqServices(configuration);
	services.AddMessageServingServices();
	services.AddMongoDbServices(configuration);
	services.AddAutoMapper(typeof(MappingProfile));
	services.AddOutboxServices();
	services.AddValidationServices();

	//-------------

	services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

	// Разбор зависимостей для репозиториев:
	builder.Services.AddTransient<IMongoRepository<QueuesEntity>, MongoRepository<QueuesEntity>>();
	builder.Services.AddTransient<IMongoRepository<OutboxMessage>, MongoRepository<OutboxMessage>>();
	builder.Services.AddTransient<IMongoRepository<IncidentEntity>, MongoRepository<IncidentEntity>>();

	services.AddSingleton<MongoRepository<OutboxMessage>>();
	services.AddSingleton<MongoRepository<QueuesEntity>>();
	services.AddSingleton<MongoRepository<IncidentEntity>>();

	services.AddHostedService<QueueListenerBackgroundService>();

	builder.Services.AddSingleton<IMongoClient>(sp =>
	{
		var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
		return new MongoClient(settings.ConnectionString);
	});

	builder.Services.AddSingleton<IMongoDatabase>(sp =>
	{
		var mongoClient = sp.GetRequiredService<IMongoClient>();
		var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];
		return mongoClient.GetDatabase(databaseName);
	});

	services.AddTransient<FileHashService>();

	// Регистрация конфигурации SftpSettings
	builder.Services.Configure<SftpSettings>(builder.Configuration.GetSection("SftpSettings"));


	services.AddSingleton<IConnectionFactory, ConnectionFactory>(_ =>
		new ConnectionFactory { HostName = "localhost" });

	builder.Services.AddScoped<IRabbitMqQueueListener<RabbitMqSftpListener>, RabbitMqSftpListener>();
	builder.Services.AddScoped<IRabbitMqQueueListener<RabbitMqQueueListener>, RabbitMqQueueListener>();

	//-------------

	var app = builder.Build();

	app.UseSerilogRequestLogging();
	app.UseCors(cors => cors.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

	var factory = app.Services.GetRequiredService<ILoggerFactory>();

	app.MapControllers();
	app.MapIntegrationMinimalApi(factory);
	app.MapAdminMinimalApi(factory);
	app.MapTestMinimalApi(factory);
	app.MapHttpBasedGetEndpoints(factory);
	app.MapHttpBasedPostEndpoints(factory);

	Log.Information("Динамический шлюз запущен и готов к эксплуатации.");	

	await app.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Критическая ошибка при запуске приложения");
	throw;
}
finally
{
	cts.Cancel();
	cts.Dispose();
	Log.CloseAndFlush();
}
