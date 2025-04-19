using servers_api.models.dynamicgatesettings.entities;
using servers_api.models.response;
using servers_api.repositories;
using servers_api.services.senders;

namespace servers_api.services.teaching
{
	/// <summary>
	/// Общий менеджер-сервис, занимающийся процессингом настройки
	/// всей инфраструктуры динамического шлюза под отдельную организацию.
	/// </summary>
	public class TeachIntegrationService(
		MongoRepository<QueuesEntity> queuesRepository,
		ITeachSenderHandler teachService,
		IQueueListenerService queueListenerService,
		IJsonParsingService jsonParsingService,
		ILogger<TeachIntegrationService> logger) : ITeachIntegrationService
	{
		public async Task<List<ResponseIntegration>> TeachAsync(CancellationToken stoppingToken)
		{
			logger.LogInformation("Начало обработки TeachAsync");

			try
			{
				//1
				logger.LogInformation("Выполняется ParseJsonAsync.");
				var parsedCombinedModel = await jsonParsingService.ParseFromConfigurationAsync(stoppingToken);

				//2 логика работы с коллекцией базы данных: 
				//если модель с такими названиями очередей существует:
				var existingQueueEntityModel = (await queuesRepository.FindAsync(x =>
					x.InQueueName == parsedCombinedModel.InQueueName &&
					x.OutQueueName == parsedCombinedModel.OutQueueName)).FirstOrDefault();

				
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
				logger.LogInformation("Сохранение в базу очередей выполнено.");

				//3 пробуем отправить сообщение в bpme
				logger.LogInformation("Выполняется ExecuteTeachAsync.");
				var apiStatus = await teachService.TeachBPMAsync(
					parsedCombinedModel,
					stoppingToken);

				//4 если мы отправили модель в bpm - тогда есть смысл начать слушать из очереди, что она нам ответила
				if (apiStatus.Result)
				{
					await queueListenerService.ExecuteAsync(stoppingToken);

					return [
					apiStatus,
					new ResponseIntegration {
						Message = $"Cлушатель очeреди {parsedCombinedModel.OutQueueName} запустился.",
						Result = true
					}
				];
				}

				return [
					apiStatus,
					new ResponseIntegration {
						Message = $"Не отправилась информация.",
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
