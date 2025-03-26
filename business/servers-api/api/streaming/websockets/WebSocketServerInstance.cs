using System.Net;
using System.Net.WebSockets;
using System.Text;
using servers_api.factory;
using servers_api.models.internallayer.instance;
using servers_api.models.response;

namespace servers_api.api.streaming.websockets

{
	public class WebSocketServerInstance : IUpServer
	{
		private readonly ILogger<WebSocketServerInstance> _logger;

		public WebSocketServerInstance(ILogger<WebSocketServerInstance> logger)
		{
			_logger = logger;
		}

		// Реализуем метод для запуска WebSocket сервера
		public async Task<ResponseIntegration> UpServerAsync(
			ServerInstanceModel instanceModel,
			CancellationToken cancellationToken = default)
		{
			var listener = new HttpListener();
			listener.Prefixes.Add($"http://{instanceModel.Host}:{instanceModel.Port}/");

			try
			{
				listener.Start();
				_logger.LogInformation("WebSocket сервер запущен на {Host}:{Port}", instanceModel.Host, instanceModel.Port);

				while (!cancellationToken.IsCancellationRequested)
				{
					// Ожидаем подключения WebSocket клиента
					var context = await listener.GetContextAsync();
					if (context.Request.IsWebSocketRequest)
					{
						var webSocketContext = await context.AcceptWebSocketAsync(null);
						_logger.LogInformation("Клиент подключен.");

						// Начинаем обработку сообщений с клиента
						await HandleWebSocketCommunication(webSocketContext.WebSocket, cancellationToken);
					}
					else
					{
						context.Response.StatusCode = 400;
						context.Response.Close();
					}
				}

				return new ResponseIntegration { Message = "Сервер запущен.", Result = true };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка запуска WebSocket сервера.");
				return new ResponseIntegration { Message = "Ошибка запуска сервера.", Result = false };
			}
		}

		// Обработка общения с клиентом через WebSocket
		private async Task HandleWebSocketCommunication(WebSocket webSocket, CancellationToken token)
		{
			byte[] buffer = new byte[1024];

			try
			{
				while (!token.IsCancellationRequested)
				{
					var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

					// Проверяем тип сообщения и обрабатываем его
					if (result.MessageType == WebSocketMessageType.Close)
					{
						// Если клиент запрашивает закрытие соединения
						await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", token);
						_logger.LogInformation("WebSocket-соединение закрыто.");
						break;
					}

					// Получаем сообщение от клиента
					string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
					_logger.LogInformation("Получено сообщение от клиента: {Message}", message);

					// Отправляем ответ клиенту
					await SendMessageAsync(webSocket, "Ответ от сервера", token);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Ошибка при общении с WebSocket: {Message}", ex.Message);
			}
		}

		// Метод для отправки сообщения клиенту
		private async Task SendMessageAsync(WebSocket webSocket, string message, CancellationToken token)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, token);
		}
	}
}
