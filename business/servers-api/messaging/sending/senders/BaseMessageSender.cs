using servers_api.listenersrabbit;
using servers_api.messaging.sending.abstractions;

namespace servers_api.messaging.sending.senders
{
	public abstract class BaseMessageSender<T> : IConnectionMessageSender
	{
		protected readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		protected readonly ILogger<T> _logger;

		protected BaseMessageSender(
			IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener,
			ILogger<T> logger)
		{
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
							await SendToClientAsync(message + "\n", cancellationToken);
							_logger.LogInformation("Сообщение отправлено клиенту: {Message}", message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка при отправке сообщения клиенту");
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в процессе отправки сообщений клиенту");
			}
		}

		protected abstract Task SendToClientAsync(string message, CancellationToken cancellationToken);
	}
}
