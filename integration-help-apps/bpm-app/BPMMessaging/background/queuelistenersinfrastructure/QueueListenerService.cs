using BPMMessaging.models.dtos;
using BPMMessaging.models.entities;
using BPMMessaging.repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;

public class QueueListenerService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<QueueListenerService> _logger;

	public QueueListenerService(IServiceProvider serviceProvider, ILogger<QueueListenerService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public async Task<List<RabbitMqQueueListener>> StartQueueListenersAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var teachingRepository = scope.ServiceProvider.GetRequiredService<IMongoRepository<TeachingEntity>>();

		var teachingEntities = await teachingRepository.GetAllAsync();
		var consumers = new ConcurrentBag<RabbitMqQueueListener>();

		await Parallel.ForEachAsync(teachingEntities, cancellationToken, async (teachingEntity, token) =>
		{
			try
			{
				using var innerScope = _serviceProvider.CreateScope();
				var connectionFactory = innerScope.ServiceProvider.GetRequiredService<IConnectionFactory>();
				var logger = innerScope.ServiceProvider.GetRequiredService<ILogger<RabbitMqQueueListener>>();
				var incidentRepository = innerScope.ServiceProvider.GetRequiredService<IMongoRepository<IncidentEntity>>();
				var outboxRepository = innerScope.ServiceProvider.GetRequiredService<IMongoRepository<OutboxMessage>>();

				var listener = new RabbitMqQueueListener(
					connectionFactory, logger, incidentRepository, outboxRepository
				);

				// создаем лисенеры согласно существующим в базе данных таблицы teaching очередям:
				await listener.StartListeningAsync(
					teachingEntity.InQueueName,
					teachingEntity.OutQueueName,
					token);
				consumers.Add(listener);

				_logger.LogInformation("Запущен слушатель для очередей {InQueue} -> {OutQueue}",
					teachingEntity.InQueueName, teachingEntity.OutQueueName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при запуске слушателя для очереди {Queue}", teachingEntity.InQueueName);
			}
		});

		return consumers.ToList();
	}
}
