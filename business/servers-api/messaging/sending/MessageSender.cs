using System.Net.Sockets;
using System.Text;
using servers_api.listenersrabbit;

namespace servers_api.messaging.sending
{
	public class MessageSender : IMessageSender
	{
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<MessageSender> _logger;

		public MessageSender(
			IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener,
			ILogger<MessageSender> logger)
		{
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessagesToClientAsync(
			TcpClient tcpClient,
			string queueForListening,
			CancellationToken cancellationToken)
		{
			try
			{
				var stream = tcpClient.GetStream();

				await _rabbitMqQueueListener.StartListeningAsync(
					queueOutName: queueForListening,
					stoppingToken: cancellationToken,
					onMessageReceived: async message =>
					{
						try
						{
							if (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Client.Available == 0)
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
				_logger.LogError(ex, "Ошибка в процессе получения сообщений из RabbitMQ.");
			}
		}
	}
}
