using Serilog;
using servers_api.api.rest.minimal.common;
using servers_api.middleware;
using servers_api.models.configurationsettings;

Console.Title = "integration api";

var builder = WebApplication.CreateBuilder(args);

LoggingConfiguration.ConfigureLogging(builder);

var (httpUrl, httpsUrl) = GateConfiguration.ConfigureDynamicGate(args, builder);

try
{
	ConfigureServices(builder);

	var app = builder.Build();
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
	var services = builder.Services;
	var configuration = builder.Configuration;
	
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
	services.AddOutboxServices();
	services.AddValidationServices();
	services.AddHostedServices();
	services.AddSftpServices(configuration);
	services.AddHeadersServices();
}

static void ConfigureApp(WebApplication app, string httpUrl, string httpsUrl)
{
	app.UseMiddleware<CompanyMiddlewareSettings>();

	app.Urls.Add(httpUrl);
	app.Urls.Add(httpsUrl);
	Log.Information($"Сервер запущен на {httpUrl} и {httpsUrl}");

	app.UseSerilogRequestLogging();
	app.UseCors(cors => cors.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

	var factory = app.Services.GetRequiredService<ILoggerFactory>();

	app.MapControllers();
	app.MapIntegrationMinimalApi(factory);
	app.MapAdminMinimalApi(factory);
	app.MapTestMinimalApi(factory);
}
