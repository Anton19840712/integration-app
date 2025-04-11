using Serilog;
using servers_api.api.streaming.clients;
using servers_api.api.streaming.core;
using servers_api.api.streaming.servers;
using servers_api.messaging.sending.abstractions;
using servers_api.middleware;

Console.Title = "integration api";

var builder = WebApplication.CreateBuilder(args);

LoggingConfiguration.ConfigureLogging(builder);

ConfigureServices(builder);


var app = builder.Build();

try
{
	// Настройка динамического шлюза (через зарегистрированный сервис)
	var gateConfigurator = app.Services.GetRequiredService<GateConfiguration>();
	var (httpUrl, httpsUrl) = await gateConfigurator.ConfigureDynamicGateAsync(args, builder);

	// Применяем настройки приложения
	ConfigureApp(app, httpUrl, httpsUrl);

	// Запускаем
	await app.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Критическая ошибка при запуске приложения");
	throw;
}
finally
{
	Log.CloseAndFlush();
}

static void ConfigureServices(WebApplicationBuilder builder)
{
	
	var configuration = builder.Configuration;

	var services = builder.Services;
	services.AddSingleton<INetworkServer, TcpNetworkServer>();
	services.AddSingleton<INetworkClient, TcpNetworkClient>();

	services.AddSingleton<INetworkServer, UdpNetworkServer>();
	services.AddSingleton<INetworkClient, UdpNetworkClient>();

	services.AddSingleton<INetworkServer, WebSocketNetworkServer>();
	services.AddSingleton<INetworkClient, WebSocketNetworkClient>();

	services.AddSingleton<NetworkServerManager>();
	services.AddSingleton<NetworkClientManager>();

	services.AddHostedService<NetworkServerHostedService>();
	services.AddScoped<ConnectionMessageSenderFactory>();

	services.AddControllers();
	services.AddAutoMapper(typeof(MappingProfile));

	services.AddCommonServices();
	services.AddHttpServices();
	services.AddFactoryServices();
	services.AddApiServices();
	services.AddRabbitMqServices(configuration);
	services.AddMessageServingServices();
	services.AddMongoDbServices(configuration);
	services.AddMongoDbRepositoriesServices(configuration);
	services.AddValidationServices();
	services.AddHostedServices();
	services.AddSftpServices(configuration);
	services.AddHeadersServices();

	// Регистрируем GateConfiguration
	services.AddSingleton<GateConfiguration>();
}

static void ConfigureApp(WebApplication app, string httpUrl, string httpsUrl)
{
	app.Urls.Add(httpUrl);
	app.Urls.Add(httpsUrl);
	Log.Information($"Middleware: динамический шлюз запущен и принимает запросы на следующих точках: {httpUrl} и {httpsUrl}");

	app.UseSerilogRequestLogging();

	app.UseCors(cors => cors
		.AllowAnyOrigin()
		.AllowAnyMethod()
		.AllowAnyHeader());

	app.MapControllers();
}
