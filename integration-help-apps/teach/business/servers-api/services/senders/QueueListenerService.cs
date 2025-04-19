using servers_api.listenersrabbit;
using servers_api.models.dynamicgatesettings.entities;
using servers_api.repositories;

namespace servers_api.services.senders
{
	public class QueueListenerService : IQueueListenerService
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<QueueListenerService> _logger;

		public QueueListenerService(IServiceScopeFactory scopeFactory, ILogger<QueueListenerService> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("QueueListenerBackgroundService: запуск фонового сервиса прослушивания очередей.");

			try
			{
				using var scope = _scopeFactory.CreateScope();
				var queueListener = scope.ServiceProvider.GetRequiredService<IRabbitMqQueueListener<RabbitMqQueueListener>>();
				var queuesRepository = scope.ServiceProvider.GetRequiredService<MongoRepository<QueuesEntity>>();

				var elements = await queuesRepository.GetAllAsync();

				if (elements == null || !elements.Any())
				{
					_logger.LogInformation("Нет конкретных очередей для прослушивания. Слушатели rabbit не будут запущены.");
					return;
				}

				foreach (var element in elements.DistinctBy(e => e.OutQueueName))
				{
					var queueName = element.OutQueueName;

					try
					{
						var success = await queueListener.StartListeningAsync(queueName, stoppingToken);

						if (success)
							_logger.LogInformation("Слушатель для очереди {Queue} запущен.", queueName);
						else
							_logger.LogWarning("Слушатель для очереди {Queue} не был запущен.", queueName);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка запуска слушателя для очереди {Queue}.", queueName);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при запуске слушателей очередей.");
			}
		}
	}
}