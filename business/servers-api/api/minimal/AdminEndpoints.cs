using rabbit_listener;
using servers_api.models.entities;
using servers_api.repositories;
using servers_api.services.brokers.bpmintegration;

namespace servers_api.api.minimal;

public static class AdminEndpoints
{
	public static void MapAdminMinimalApi(
		this IEndpointRouteBuilder app,
		ILoggerFactory loggerFactory)
	{
		var logger = loggerFactory.CreateLogger("AdminEndpoints");
		
		// зачитываем известные системе (берем их названия из базы) очереди,
		// если в них что-то осталось и нам нужно их очистить полностью под какие-то дальнейшие наши задачи
		app.MapGet("/api/consume", async (
			IRabbitMqQueueListener<RabbitMqQueueListener> queueListener,
			MongoRepository<QueuesEntity> queuesRepository,
			CancellationToken stoppingToken) =>
		{
			try
			{
				logger.LogInformation("Dumping messages from all queues.");

				// Получаем названия всех очередей из репозитория:
				var elements = await queuesRepository.GetAllAsync();

				foreach (var element in elements)
				{
					try
					{
						// Для каждой очереди запускаем слушателя в отдельной задаче:
						await queueListener.StartListeningAsync(element.OutQueueName, stoppingToken);
					}
					catch (Exception ex)
					{
						// Логируем ошибку для каждой очереди отдельно, но продолжаем обработку других:
						logger.LogError(ex, "Error retrieving messages from queue: {QueueName}", element.OutQueueName);
					}
				}

				logger.LogInformation("Процесс получения сообщений из очередей завершен.");
				return Results.Ok();
			}


			catch (Exception ex)
			{
				logger.LogError(ex, "Error while getting messages from queues");
				return Results.Problem(ex.Message);
			}
		});

		// здесь мы получить файлы, сохраненные в очереди под sftp соединение
		// данные файлы не потребляются на стороне bpm они засылаются в очередь
		// так же файлы паралелльно шлются в sftp server
		app.MapGet("/api/consume-sftp", async (
			string queueSftpName,
			string pathToSave,
			IRabbitMqQueueListener<RabbitMqSftpListener> queueListener,
			CancellationToken stoppingToken) =>
		{
			try
			{
				logger.LogInformation("Запуск прослушивания очереди {Queue}", queueSftpName);

				await queueListener.StartListeningAsync(queueSftpName, stoppingToken, pathToSave);

				logger.LogInformation("Процесс получения сообщений из очереди {Queue} завершен.", queueSftpName);
				return Results.Ok();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка при получении сообщений из очереди {Queue}", queueSftpName);
				return Results.Problem(ex.Message);
			}
		});
	}
}
