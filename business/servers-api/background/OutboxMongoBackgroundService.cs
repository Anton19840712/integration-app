﻿using servers_api.models.outbox;
using servers_api.repositories;

public class OutboxMongoBackgroundService : BackgroundService
{
	private readonly MongoRepository<OutboxMessage> _outboxRepository;
	private readonly IRabbitMqService _rabbitMqService;
	private readonly ILogger<OutboxMongoBackgroundService> _logger;

	public OutboxMongoBackgroundService(
		MongoRepository<OutboxMessage> outboxRepository,
		IRabbitMqService rabbitMqService,
		ILogger<OutboxMongoBackgroundService> logger)
	{
		_outboxRepository = outboxRepository;
		_rabbitMqService = rabbitMqService;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		_logger.LogInformation("Фоновый процесс по обработке сообщений из outbox table запущен.");

		//Параллельно запускаем фоновую очистку старых сообщений
		_ = Task.Run(() => CleanupOldMessagesAsync(token), token);

		while (!token.IsCancellationRequested)
		{
			try
			{
				var messages = await _outboxRepository.GetUnprocessedMessagesAsync();

				foreach (var message in messages)
				{
					_logger.LogInformation($"Публикация сообщения: {message.Payload}");

					await _rabbitMqService.PublishMessageAsync(
						message.InQueue,
						message.RoutingKey,
						message.Payload);

					await _outboxRepository.MarkMessageAsProcessedAsync(message.Id);

					_logger.LogInformation($"Обработано в Outbox: {message.Payload}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при обработке Outbox.");
			}

			// Задержка между обработками сообщений, можно изменить на нужное значение
			await Task.Delay(2000, token);
		}
	}

	private async Task CleanupOldMessagesAsync(CancellationToken token)
	{
		const int ttlDifference = 10;  // Установите желаемый интервал сущестования объекта в базе данных.
		const int intervalInSeconds = 10;  // Установите желаемый интервал для повторной проверки сообщений, которые требуется удалить.

		while (!token.IsCancellationRequested)
		{
			try
			{
				int deletedCount = await _outboxRepository.DeleteOldMessagesAsync(TimeSpan.FromSeconds(ttlDifference));
				if (deletedCount!=0)
				{
					_logger.LogInformation($"OutboxMongoBackgroundService: yдалено {deletedCount} старых сообщений из базы GatewayDB, коллекции outbox_messages.");
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
}
