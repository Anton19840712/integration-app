using servers_api.listenersrabbit;
using System.Net.WebSockets;
using System.Text;

namespace servers_api.messaging.sending
{
	public class WebSocketMessageSender : IConnectionMessageSender
	{
		private readonly WebSocket _socket;
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<WebSocketMessageSender> _logger;

		public WebSocketMessageSender(WebSocket socket, IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener, ILogger<WebSocketMessageSender> logger)
		{
			_socket = socket;
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken)
		{
			try
			{
				await _rabbitMqQueueListener.StartListeningAsync(
					queueOutName: queueForListening,
					stoppingToken: cancellationToken,
					onMessageReceived: async message =>
					{
						try
						{
							var buffer = Encoding.UTF8.GetBytes(message + "\n");
							await _socket.SendAsync(
								new ArraySegment<byte>(buffer),
								WebSocketMessageType.Text,
								endOfMessage: true,
								cancellationToken);

							_logger.LogInformation("Сообщение отправлено WebSocket-клиенту: {Message}", message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка при отправке сообщения WebSocket-клиенту");
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в процессе отправки сообщений WebSocket-клиенту");
			}
		}
	}
}
