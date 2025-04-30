using servers_api.models.dynamicgatesettings.entities;
using servers_api.models.response;
using servers_api.repositories;

namespace servers_api.services.teaching
{
	/// <summary>
	/// Общий менеджер-сервис, занимающийся процессингом настройки
	/// всей инфраструктуры динамического шлюза под отдельную организацию.
	/// </summary>
	public class TeachIntegrationService(
		MongoRepository<QueuesEntity> queuesRepository,
		IJsonParsingService jsonParsingService,
		ILogger<TeachIntegrationService> logger) : ITeachIntegrationService
	{
		public async Task<List<ResponseIntegration>> TeachAsync(CancellationToken stoppingToken)
		{
			logger.LogInformation("Начало обработки TeachAsync");

			try
			{
				//1:
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

				return [
					new ResponseIntegration {
						Message = $"Очереди были сохранены в базу успешно.",
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
