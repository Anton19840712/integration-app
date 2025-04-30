using RabbitMQ.Client.Events;
using RabbitMQ.Client;

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

		public async Task StartListeningAsync(
			string queueOutName,
			CancellationToken stoppingToken,
			string pathForSave = null,
			Func<string, Task> onMessageReceived = null) // <--- сюда можно передать обработку сообщений
		{
			_channel = _connection.CreateModel();

			while (!QueueExists(_channel, queueOutName))
			{
				_logger.LogWarning("Очередь {Queue} не найдена. Ожидание...", queueOutName);
				await Task.Delay(1000, stoppingToken);
			}

			var consumer = new EventingBasicConsumer(_channel);

			consumer.Received += async (model, ea) =>
			{
				var message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

				_logger.LogInformation("Получено сообщение из {Queue}: {Message}", queueOutName, message);

				if (onMessageReceived != null)
				{
					await onMessageReceived(message);
				}
				else
				{
					await ProcessMessageAsync(message, queueOutName);
				}
			};

			_logger.LogInformation("Подключен к очереди {Queue}. Ожидание сообщений...", queueOutName);
			_channel.BasicConsume(queue: queueOutName, autoAck: true, consumer: consumer);

			try
			{
				await Task.Delay(Timeout.Infinite, stoppingToken);
			}
			catch (TaskCanceledException)
			{
				_logger.LogInformation("Остановка слушателя очереди {Queue}.", queueOutName);
			}
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
			catch
			{
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
