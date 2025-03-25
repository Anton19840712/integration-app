using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Program
{
	public static async Task Main(string[] args)
	{
		Console.Title = "http-test-client";
		using var host = Host.CreateDefaultBuilder(args)
			.ConfigureServices(services =>
			{
				services.AddHostedService<HttpClientService>();
			})
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddConsole();
			})
			.Build();

		await host.RunAsync();
	}
}

public class HttpClientService : IHostedService
{
	private readonly ILogger<HttpClientService> _logger;
	private readonly HttpClient _httpClient = new HttpClient();
	private CancellationTokenSource _cts;
	private Task _clientTask;
	private const string ServerUrl = "http://127.0.0.1:5001/";

	public HttpClientService(ILogger<HttpClientService> logger)
	{
		_logger = logger;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Запуск HTTP-клиента...");
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_clientTask = Task.Run(() => ConnectToServerAsync(_cts.Token), _cts.Token);
		return Task.CompletedTask;
	}

	private async Task ConnectToServerAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			try
			{
				_logger.LogInformation($"Отправка запроса на {ServerUrl}...");
				var response = await _httpClient.GetStringAsync(ServerUrl);
				_logger.LogInformation($"Ответ от сервера: {response}");

				// Логирование успешного соединения
				_logger.LogInformation("Соединение с сервером успешно установлено.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Ошибка: {ex.Message}");
				_logger.LogWarning("Соединение с сервером разорвано или недоступно.");
			}

			await Task.Delay(5000, token); // Повторный запрос через 5 сек
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Остановка HTTP-клиента...");
		_cts?.Cancel();
		return _clientTask ?? Task.CompletedTask;
	}
}
