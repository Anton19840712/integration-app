using servers_api.listenersrabbit;
using System.Net.Sockets;
using System.Text;

namespace servers_api.messaging.sending
{
	public class TcpMessageSender : IConnectionMessageSender
	{
		private readonly TcpClient _tcpClient;
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<TcpMessageSender> _logger;

		public TcpMessageSender(TcpClient tcpClient, IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener, ILogger<TcpMessageSender> logger)
		{
			_tcpClient = tcpClient;
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken)
		{
			try
			{
				var stream = _tcpClient.GetStream();
				await _rabbitMqQueueListener.StartListeningAsync(
					queueOutName: queueForListening,
					stoppingToken: cancellationToken,
					onMessageReceived: async message =>
					{
						try
						{
							if (_tcpClient.Client.Poll(0, SelectMode.SelectRead) && _tcpClient.Client.Available == 0)
							{
								_logger.LogWarning("TCP-клиент отключился (низкоуровневая проверка).");
								return;
							}

							byte[] data = Encoding.UTF8.GetBytes(message + "\n");
							await stream.WriteAsync(data, 0, data.Length, cancellationToken);
							await stream.FlushAsync(cancellationToken);

							_logger.LogInformation("Сообщение отправлено TCP-клиенту: {Message}", message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка при отправке сообщения TCP-клиенту");
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в процессе отправки сообщений TCP-клиенту");
			}
		}
	}
}
