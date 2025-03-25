using System.Net.WebSockets;
using System.Text;
using servers_api.factory;
using servers_api.models.internallayer.instance;
using servers_api.models.response;

public class WebSocketClientInstance : IUpClient
{
	private readonly ILogger<WebSocketClientInstance> _logger;
	private ClientWebSocket _webSocket;

	public WebSocketClientInstance(ILogger<WebSocketClientInstance> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<ResponseIntegration> ConnectToServerAsync(ClientInstanceModel instanceModel, string serverHost, int serverPort, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Запуск WebSocket клиента...");
		_webSocket = new ClientWebSocket();
		_webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);  // Устанавливаем интервал keep-alive
		var uri = new Uri($"ws://{serverHost}:{serverPort}");

		try
		{
			await _webSocket.ConnectAsync(uri, cancellationToken);
			_logger.LogInformation("Успешное подключение к WebSocket!");

			// Запускаем прием сообщений от сервера в отдельном потоке
			_ = ReceiveMessagesAsync(cancellationToken);

			return new ResponseIntegration { Message = "Успешное подключение.", Result = true };
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Получение сообщений отменено.");
			return new ResponseIntegration { Message = "Операция отменена.", Result = false };
		}
		catch (Exception ex)
		{
			_logger.LogError("Ошибка WebSocket клиента: {Message}", ex.Message);
			return new ResponseIntegration { Message = "Ошибка подключения.", Result = false };
		}
	}

	private async Task ReceiveMessagesAsync(CancellationToken token)
	{
		byte[] buffer = new byte[1024];

		try
		{
			while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
			{
				var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					_logger.LogInformation("WebSocket-соединение закрыто сервером.");
					break;
				}

				string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				if (message == "Ping")
				{
					_logger.LogInformation("Получен пинг от сервера. Отправляю ответ.");
					await SendPongAsync(); // Отправляем ответ на пинг
				}
				else
				{
					_logger.LogInformation("Получено сообщение: {Message}", message);
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Операция отменена.");
		}
		catch (Exception ex)
		{
			_logger.LogError("Ошибка при получении сообщений: {Message}", ex.Message);
		}
	}

	private async Task SendPongAsync()
	{
		try
		{
			byte[] pongMessage = Encoding.UTF8.GetBytes("Pong");
			await _webSocket.SendAsync(new ArraySegment<byte>(pongMessage), WebSocketMessageType.Text, true, CancellationToken.None);
			_logger.LogInformation("Отправлен ответ 'Pong' на пинг.");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Ошибка при отправке 'Pong': {ex.Message}");
		}
	}
}