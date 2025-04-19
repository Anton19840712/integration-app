using Serilog;
using servers_api.middleware;
using servers_api.services.teaching;

Console.Title = "integration api";

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder);

var app = builder.Build();

try
{
	var gateConfigurator = app.Services.GetRequiredService<GateConfiguration>();
	var (httpUrl, httpsUrl) = await gateConfigurator.ConfigureDynamicGateAsync(args, builder);

	//Запускаем интеграцию
	using var scope = app.Services.CreateScope();
	var teachIntegrationService = scope.ServiceProvider.GetRequiredService<ITeachIntegrationService>();
	await teachIntegrationService.TeachAsync(CancellationToken.None);

	ConfigureApp(app, httpUrl, httpsUrl);
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
	LoggingConfiguration.ConfigureLogging(builder);

	var configuration = builder.Configuration;
	var services = builder.Services;

	services.AddTransient<ITeachIntegrationService, TeachIntegrationService>();

	// Регистрируем GateConfiguration
	services.AddSingleton<GateConfiguration>();
	services.AddHttpClient();
	services.AddControllers();
	services.AddTransient<ITeachSenderHandler, TeachSenderHandler>();
	services.AddCommonServices();
	services.AddApiServices();
	services.AddRabbitMqServices(configuration);
	services.AddMongoDbServices(configuration);
	services.AddMongoDbRepositoriesServices(configuration);
	services.AddHostedServices();
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
