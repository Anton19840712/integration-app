using rtsp_dynamic_gate_app.background;
using rtsp_dynamic_gate_app.middleware;
using rtsp_dynamic_gate_app.models;
using Serilog;

//предполагается, что приложение реализовано для обслуживания тяжелых и быстрых запросов. Highload.
//в нем будет нужно развивать все протоколы, которые будут принимать большое количество тяжелых rps.

Console.Title = "highload-rtsp-requests-gate";

var builder = WebApplication.CreateBuilder(args);

// Настроим логирование
LoggingConfiguration.ConfigureLogging(builder);

// Загрузка конфигурации и настройка URL
var configLoader = new GateConfiguration();
var (httpUrl, httpsUrl) = configLoader.ConfigureRtspGate(args, builder);

// Конфигурация RTSP-сервиса
builder.Services.Configure<RtspSettings>(builder.Configuration.GetSection("RtspSettings"));
builder.Services.AddControllers();
builder.Services.AddSingleton<RtspStreamingService>(); // Создаёт и регает сам экземпляр
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RtspStreamingService>());

// Добавление CORS
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

var app = builder.Build();

try
{
	app.MapControllers();

	app.Urls.Add(httpUrl);
	app.Urls.Add(httpsUrl);

	Log.Information("Middleware: динамический шлюз запущен на {HttpUrl} и {HttpsUrl}", httpUrl, httpsUrl);

	// Логирование запросов с Serilog
	app.UseSerilogRequestLogging();

	// Включаем CORS в пайплайне
	app.UseCors(); // Этот вызов должен быть после UseRouting, но до UseAuthorization

	// Запускаем приложение
	await app.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Критическая ошибка при запуске приложения");
}
finally
{
	Log.CloseAndFlush();
}
