using Serilog;
using servers_api.middleware;
using servers_api.models.dynamicgatesettings.parameters;

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
	Log.Fatal(ex, "����������� ������ ��� ������� ����������");
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
	services.AddValidationServices();
	services.AddHostedServices();
	services.AddSftpServices(configuration);
	services.AddHeadersServices();
}

static void ConfigureApp(WebApplication app, string httpUrl, string httpsUrl)
{
	app.UseMiddleware<ParamsToContextSettings>();

	app.Urls.Add(httpUrl);
	app.Urls.Add(httpsUrl);
	Log.Information($"������ ������� �� {httpUrl} � {httpsUrl}");

	app.UseSerilogRequestLogging();
	app.UseCors(cors => cors.AllowAnyOrigin()
							.AllowAnyMethod()
							.AllowAnyHeader());

	var factory = app.Services.GetRequiredService<ILoggerFactory>();

	app.MapControllers();
}
