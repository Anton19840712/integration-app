using System.Text.Json;
using servers_api.listenersrabbit;
using servers_api.models.dynamicgatesettings.entities;
using servers_api.models.response;
using servers_api.repositories;
using servers_api.Services.Parsers;

namespace servers_api.services.teaching
{
	/// <summary>
	/// Общий менеджер-сервис, занимающийся процессингом настройки
	/// всей инфраструктуры динамического шлюза под отдельную организацию.
	/// </summary>
	public class TeachIntegrationService(
		MongoRepository<QueuesEntity> queuesRepository,
		ITeachSenderHandler teachService,
		IJsonParsingService jsonParsingService,
		IRabbitMqQueueListener<RabbitMqQueueListener> queueListener,
		ILogger<TeachIntegrationService> logger) : ITeachIntegrationService
	{
		public async Task<List<ResponseIntegration>> TeachAsync(
			JsonElement jsonBody,
			CancellationToken stoppingToken)
		{
			logger.LogInformation("Начало обработки TeachAsync");

			try
			{
				//1
				logger.LogInformation("Выполняется ParseJsonAsync.");
				var parsedCombinedModel = await jsonParsingService.ParseJsonAsync(
					jsonBody,
					isTeaching: true,
					stoppingToken);

				//2 логика работы с коллекцией базы данных: 
				//если модель с такими названиями очередей существует:
				var existingQueueEntityModel = (await queuesRepository.FindAsync(x =>
					x.InQueueName == parsedCombinedModel.InQueueName &&
					x.OutQueueName == parsedCombinedModel.OutQueueName)).FirstOrDefault();

				logger.LogInformation("Выполняется сохранение в базу очередей.");
				var incomingQueuesEntitySave = new QueuesEntity()
				{
					InQueueName = parsedCombinedModel.InQueueName,
					OutQueueName = parsedCombinedModel.OutQueueName
				};

				if (existingQueueEntityModel != null)
				{
					await queuesRepository.UpdateAsync(
						existingQueueEntityModel.Id,
						incomingQueuesEntitySave);
				}
				else
				{
					// Если модели нет — вставляем эту новую:
					await queuesRepository.InsertAsync(incomingQueuesEntitySave);
				}

				//3
				logger.LogInformation("Выполняется ExecuteTeachAsync.");
				var apiStatus = await teachService.TeachBPMAsync(
					parsedCombinedModel,
					stoppingToken);

				//4
				logger.LogInformation("Запускаем слушателя в фоне для очереди: {Queue}.", parsedCombinedModel.OutQueueName);

				await queueListener.StartListeningAsync(
				parsedCombinedModel.OutQueueName,
				stoppingToken);

				return [
					apiStatus,
					new ResponseIntegration {
						Message = $"Cлушатель очeреди {parsedCombinedModel.OutQueueName} запустился.",
						Result = true
					}
				];
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка в процессе TeachAsync");
				throw;
			}
		}
	}
}
