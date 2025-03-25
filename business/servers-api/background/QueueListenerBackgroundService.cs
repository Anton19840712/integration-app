using servers_api.models.entities;
using servers_api.repositories;
using servers_api.services.brokers.bpmintegration;

public class QueueListenerBackgroundService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<QueueListenerBackgroundService> _logger;

	public QueueListenerBackgroundService(IServiceScopeFactory scopeFactory, ILogger<QueueListenerBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Запуск фонового сервиса прослушивания очередей.");

		try
		{
			using var scope = _scopeFactory.CreateScope();
			var queueListener = scope.ServiceProvider.GetRequiredService<IRabbitMqQueueListener<RabbitMqQueueListener>>();
			var queuesRepository = scope.ServiceProvider.GetRequiredService<MongoRepository<QueuesEntity>>();

			var elements = await queuesRepository.GetAllAsync();

			var listeningTasks = elements
				.Select(element => Task.Run(() => queueListener.StartListeningAsync(element.OutQueueName, stoppingToken), stoppingToken))
				.ToList();

			await Task.WhenAll(listeningTasks);

			_logger.LogInformation("Все слушатели запущены.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при запуске слушателей очередей.");
		}
	}
}
