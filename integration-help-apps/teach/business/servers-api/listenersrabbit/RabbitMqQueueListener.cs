using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace servers_api.listenersrabbit
{
	public class RabbitMqQueueListener : IRabbitMqQueueListener<RabbitMqQueueListener>
	{
		private readonly ILogger<RabbitMqQueueListener> _logger;
		private readonly IConnection _connection;
		private IModel _channel;

		public RabbitMqQueueListener(IRabbitMqService rabbitMqService, ILogger<RabbitMqQueueListener> logger)
		{
			_connection = rabbitMqService.CreateConnection();
			_logger = logger;
		}

		public async Task<bool> StartListeningAsync(
			string queueOutName,
			CancellationToken stoppingToken,
			string pathForSave = null,
			Func<string, Task> onMessageReceived = null)
		{
			_channel = _connection.CreateModel();

			const int maxAttempts = 5;

			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				if (QueueExists(_channel, queueOutName))
				{
					_logger.LogInformation("Очередь {Queue} найдена на попытке {Attempt}.", queueOutName, attempt);
					break;
				}

				if (attempt < maxAttempts)
				{
					_logger.LogWarning("Очередь {Queue} не найдена. Попытка {Attempt} из {MaxAttempts}.", queueOutName, attempt, maxAttempts);
					await Task.Delay(1000, stoppingToken);
				}
				else
				{
					_logger.LogWarning("Очередь {Queue} не найдена. Попытка {Attempt} из {MaxAttempts}.", queueOutName, attempt, maxAttempts);
					_logger.LogError("Очередь {Queue} не найдена после {MaxAttempts} попыток. Слушатель НЕ был запущен.", queueOutName, maxAttempts);
					return false;
				}
			}

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += async (model, ea) =>
			{
				var message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
				_logger.LogInformation("Получено сообщение из {Queue}: {Message}", queueOutName, message);

				if (onMessageReceived != null)
					await onMessageReceived(message);
				else
					await ProcessMessageAsync(message, queueOutName);
			};

			_logger.LogInformation("Подключен к очереди {Queue}. Ожидание сообщений...", queueOutName);
			_channel.BasicConsume(queue: queueOutName, autoAck: true, consumer: consumer);

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(Timeout.Infinite, stoppingToken);
				}
				catch (TaskCanceledException)
				{
					_logger.LogInformation("Остановка слушателя очереди {Queue}.", queueOutName);
				}
			});

			return true;
		}

		protected virtual Task ProcessMessageAsync(string message, string queueName)
		{
			_logger.LogInformation("Обработка сообщения из {Queue}: {Message}", queueName, message);
			return Task.CompletedTask;
		}

		private bool QueueExists(IModel channel, string queueName)
		{
			try
			{
				channel.QueueDeclarePassive(queueName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogDebug(ex, "QueueDeclarePassive вызвал исключение для очереди {Queue}", queueName);
				return false;
			}
		}

		public void StopListening()
		{
			_logger.LogInformation("Остановка RabbitMQ слушателя...");
			_channel?.Close();
		}
	}
}
