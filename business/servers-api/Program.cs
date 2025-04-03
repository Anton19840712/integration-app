using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RabbitMQ.Client;
using Serilog;
using servers_api.api.rest.minimal.common;
using servers_api.messaging.processing;
using servers_api.middleware;
using servers_api.models.configurationsettings;
using servers_api.models.entities;
using servers_api.models.outbox;
using servers_api.repositories;
using servers_api.services.brokers.bpmintegration;
using servers_api.validation.headers;

Console.Title = "integration api";

// Загружаем настройки из launchSettings.json и environment variables
var builder = WebApplication.CreateBuilder(args);

// Получаем параметры из командной строки
var port = GetArgument(args, "--port=", GetConfigValue(builder, "applicationUrl", 5000));// если не было ничего передано, будем запускаться на 5000 http
var host = GetArgument(args, "--host=", "localhost");
var enableValidation = GetArgument(args, "--validate=", true);
var companyName = GetArgument(args, "--company=", "default-company");

// Зафиксируем параметры в конфигурации:
builder.Configuration["CompanyName"] = companyName;
builder.Configuration["Host"] = host;
builder.Configuration["Port"] = port.ToString();
builder.Configuration["Validate"] = enableValidation.ToString();

builder.Services.Configure<CompanyMiddlewareSettings>(builder.Configuration);


//---------
// Метод для парсинга аргументов командной строки
static T GetArgument<T>(string[] args, string key, T defaultValue)
{
	var arg = args.FirstOrDefault(a => a.StartsWith(key));
	if (arg != null)
	{
		var value = arg.Substring(key.Length);
		try
		{
			return (T)Convert.ChangeType(value, typeof(T));
		}
		catch
		{
			Console.WriteLine($"Ошибка при разборе аргумента {key}. Используется значение по умолчанию: {defaultValue}");
		}
	}
	return defaultValue;
}
// Метод для получения значения из конфигурации (например, из launchSettings.json)
static T GetConfigValue<T>(WebApplicationBuilder builder, string key, T defaultValue)
{
	var configValue = builder.Configuration[key];
	if (configValue != null)
	{
		try
		{
			return (T)Convert.ChangeType(configValue, typeof(T));
		}
		catch
		{
			Console.WriteLine($"Ошибка при разборе конфигурации по ключу {key}. Используется значение по умолчанию: {defaultValue}");
		}
	}
	return defaultValue;
}

 
builder.Services.AddTransient<IHeadersValidator, SimpleHeadersValidator>();
builder.Services.AddTransient<IHeadersValidator, DetailedHeadersValidator>();
builder.Services.AddTransient<IHeaderValidationService, HeaderValidationService>();

// Создание токенов отмены
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

	// Валидация заголовков:
	builder.Services.AddTransient<IHeaderValidationService, HeaderValidationService>();
	builder.Services.AddTransient<IMessageProcessingService, MessageProcessingService>();

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

	// Регистрация конфигурации SftpSettings:
	builder.Services.Configure<SftpSettings>(builder.Configuration.GetSection("SftpSettings"));


	services.AddSingleton<IConnectionFactory, ConnectionFactory>(_ =>
		new ConnectionFactory { HostName = "localhost" });

	builder.Services.AddScoped<IRabbitMqQueueListener<RabbitMqSftpListener>, RabbitMqSftpListener>();
	builder.Services.AddScoped<IRabbitMqQueueListener<RabbitMqQueueListener>, RabbitMqQueueListener>();



	//-------------
	// Установка параметров из командной строки:


	var app = builder.Build();
	app.UseMiddleware<CompanyMiddlewareSettings>();

	// Устанавливаем URL с переданными параметрами:
	var url = $"http://{host}:{port}";
	app.Urls.Add(url);

	Console.WriteLine($"Сервер запущен на {url}");
	app.UseSerilogRequestLogging();

	app.UseCors(cors => cors.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

	var factory = app.Services.GetRequiredService<ILoggerFactory>();

	app.MapControllers();
	app.MapIntegrationMinimalApi(factory);
	app.MapAdminMinimalApi(factory);
	app.MapTestMinimalApi(factory);

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
