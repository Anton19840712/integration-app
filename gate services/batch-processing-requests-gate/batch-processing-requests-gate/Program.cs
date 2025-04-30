using Serilog;
using sftp_dynamic_gate_app.middleware;
using sftp_dynamic_gate_app.models;

Console.Title = "batch-processing-requests-gate";

var builder = WebApplication.CreateBuilder(args);

LoggingConfiguration.ConfigureLogging(builder);

ConfigureServices(builder);

var app = builder.Build();

try
{
	// Настройка динамического шлюза (через зарегистрированный сервис)
	var gateConfigurator = app.Services.GetRequiredService<GateConfiguration>();
	var (httpUrl, httpsUrl) = gateConfigurator.ConfigureSftpGate(args, builder);

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

	services.AddSingleton<GateConfiguration>();
	services.AddRabbitMqServices(configuration);
	services.AddSftpServices(configuration);

	services.AddControllers();
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
