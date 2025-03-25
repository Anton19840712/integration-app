using MongoDB.Driver;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BPMMessaging.models.dtos;
using BPMMessaging.publishing;
using Microsoft.Extensions.DependencyInjection;
using BPMMessaging.repository;

namespace BPMIntegration.Services.Background
{
	/// <summary>
	/// Данный сервис целенаправленно каждые 5000 мс собирает информацию из outbox table на предмет того, запроцешенно ли сообщение или нет.
	/// После этого публикует незапроцешенные сообщение в out очередь, которую слушает динамический шлюз с другой стороны.
	/// Далее сообщение заслушается на стороне сервера интеграционного шлюза из очереди реббит шины и выведется в консоль
	/// </summary>
	namespace BPMIntegration.Services.Background
	{
		public class OutboxIntegrationTrackingService : IHostedService
		{
			private readonly IMongoRepository<OutboxMessage> _outboxRepository;
			private readonly IServiceScopeFactory _serviceScopeFactory;
			private readonly ILogger<OutboxIntegrationTrackingService> _logger;

			public OutboxIntegrationTrackingService(
				IMongoRepository<OutboxMessage> outboxRepository,
				IServiceScopeFactory serviceScopeFactory,
				ILogger<OutboxIntegrationTrackingService> logger)
			{
				_outboxRepository = outboxRepository;
				_serviceScopeFactory = serviceScopeFactory;
				_logger = logger;
			}

			public Task StartAsync(CancellationToken cancellationToken)
			{
				_logger.LogInformation("OutboxProcessorService запущен.");

				// Параллельно запускаем фоновую очистку старых сообщений
				// _ = Task.Run(() => CleanupOldMessagesAsync(cancellationToken), cancellationToken);

				_ = Task.Run(async () =>
				{
					while (!cancellationToken.IsCancellationRequested)
					{
						try
						{
							await ProcessOutboxMessagesAsync(cancellationToken);
						}
						catch (OperationCanceledException)
						{
							_logger.LogInformation("Обработка сообщений была отменена.");
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка в процессе обработки сообщений.");
						}

						await Task.Delay(5000, cancellationToken);
					}
				}, cancellationToken);

				return Task.CompletedTask;
			}

			public Task StopAsync(CancellationToken cancellationToken)
			{
				_logger.LogInformation("OutboxProcessorService остановлен.");
				return Task.CompletedTask;
			}
			private async Task CleanupOldMessagesAsync(CancellationToken token)
			{
				const int ttlDifference = 10;  // Установите желаемый интервал сущестования объекта в базе данных.
				const int intervalInSeconds = 10;  // Установите желаемый интервал для повторной проверки сообщений, которые требуется удалить.

				while (!token.IsCancellationRequested)
				{
					try
					{
						int deletedCount = await _outboxRepository.DeleteByTtlAsync(TimeSpan.FromSeconds(ttlDifference));
						if (deletedCount != 0)
						{
							_logger.LogInformation($"OutboxMongoBackgroundService: yдалено {deletedCount} старых сообщений из базы ProtocolEvenrtsDB, коллекции outbox_messages.");
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка при очистке старых сообщений Outbox.");
					}

					// Запуск очистки каждые intervalInSeconds
					await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds), token);
				}
			}
			private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
			{
				using var scope = _serviceScopeFactory.CreateScope();
				var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
				var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

				var collection = mongoDatabase.GetCollection<OutboxMessage>("OutboxMessages");

				try
				{
					var filter = Builders<OutboxMessage>.Filter.Eq(m => m.IsProcessed, false);
					var outboxMessages = await collection.Find(filter).ToListAsync(cancellationToken);

					if (outboxMessages.Any())
					{
						_logger.LogInformation($"В таблице outbox-message найдено сообщений: {outboxMessages.Count} для обработки.");

						foreach (var message in outboxMessages)
						{
							cancellationToken.ThrowIfCancellationRequested();

							// 1 публикуем сообщение:
							await messagePublisher.PublishAsync(message.OutQueue, message, cancellationToken);

							// 2 обновляем в памяти, что оно IsProcessed:
							var update = Builders<OutboxMessage>.Update.Set(m => m.IsProcessed, true);

							// 3 сохраняем обновление в базе:
							await collection.UpdateOneAsync(Builders<OutboxMessage>.Filter.Eq(m => m.Id, message.Id), update);

							_logger.LogInformation($"Сообщение outbox с id {message.Id} отправлено в очередь {message.OutQueue}.");
						}

						_logger.LogInformation("Все сообщения успешно обработаны.");
					}
					else
					{
						_logger.LogInformation("Коллекция OutboxMessages не содержит необработанных сообщений.");
					}
				}
				catch (OperationCanceledException)
				{
					_logger.LogInformation("Обработка сообщений была отменена.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Ошибка при обработке сообщений в очереди.");
				}
			}
		}
	}
}
