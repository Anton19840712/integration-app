using servers_api.api.rest.test;
using servers_api.messaging.processing;
using System.Net.Sockets;
using System.Text;

public class TcpNetworkClient : INetworkClient
{
	private readonly ILogger<TcpNetworkClient> _logger;
	private readonly IMessageProcessingService _messageProcessingService;
	private readonly string _host;
	private readonly int _port;
	private readonly string _outQueue;
	private readonly string _inQueue;
	private CancellationTokenSource _cts;
	private Task _clientTask;

	private const int MaxDelayMilliseconds = 60000; // максимум 1 минута между попытками

	public TcpNetworkClient(
		ILogger<TcpNetworkClient> logger,
		IMessageProcessingService messageProcessingService,
		IConfiguration configuration)
	{
		_logger = logger;
		_messageProcessingService = messageProcessingService;

		_host = configuration["host"] ?? "localhost";
		_port = int.TryParse(configuration["port"], out var p) ? p : 5019;

		var companyName = configuration["CompanyName"] ?? "default";
		_outQueue = companyName + "_out";
		_inQueue = companyName + "_in";
	}

	public string Protocol => "tcp";
	public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (IsRunning) return Task.CompletedTask;
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_clientTask = Task.Run(() => RunClientLoopAsync(_cts.Token), _cts.Token);
		return Task.CompletedTask;
	}

	private async Task RunClientLoopAsync(CancellationToken token)
	{
		int attempt = 0;

		while (!token.IsCancellationRequested)
		{
			try
			{
				using var client = new TcpClient();
				await client.ConnectAsync(_host, _port);
				_logger.LogInformation("[TCP Client] Подключено к серверу {Host}:{Port}", _host, _port);

				using var stream = client.GetStream();
				var buffer = new byte[1024];
				attempt = 0; // сброс при успешном подключении

				while (!token.IsCancellationRequested)
				{
					var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length, token);
					if (byteCount == 0)
					{
						_logger.LogWarning("[TCP Client] Сервер закрыл соединение");
						break;
					}

					var message = Encoding.UTF8.GetString(buffer, 0, byteCount);
					_logger.LogInformation("[TCP Client] Получено сообщение: {Message}", message);

					await _messageProcessingService.ProcessIncomingMessageAsync(
						message: message,
						instanceModelQueueOutName: _outQueue,
						instanceModelQueueInName: _inQueue,
						host: _host,
						port: _port,
						protocol: "tcp");
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("[TCP Client] Остановка по токену отмены");
				break;
			}
			catch (SocketException ex)
			{
				attempt++;
				int delay = Math.Min(1000 * (int)Math.Pow(2, attempt), MaxDelayMilliseconds);
				_logger.LogWarning("[TCP Client] Попытка {Attempt}: не удалось подключиться к {Host}:{Port} — {Message}. Повтор через {Delay} мс",
					attempt, _host, _port, ex.Message, delay);
				await SafeDelayAsync(delay, token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[TCP Client] Критическая ошибка. Клиент остановлен.");
				break;
			}
		}
	}

	private async Task SafeDelayAsync(int delayMs, CancellationToken token)
	{
		try
		{
			await Task.Delay(delayMs, token);
		}
		catch (TaskCanceledException)
		{
			// Игнорируем — это ожидаемо при отмене
		}
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (!IsRunning) return;

		_cts.Cancel();

		try
		{
			await _clientTask;
		}
		catch (TaskCanceledException)
		{
			_logger.LogInformation("[TCP Client] Клиент остановлен");
		}
	}
}
