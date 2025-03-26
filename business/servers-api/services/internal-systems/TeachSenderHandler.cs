using System.Text;
using Newtonsoft.Json;
using servers_api.models.internallayer.common;
using servers_api.models.queues;
using servers_api.models.response;

namespace servers_api.Services.InternalSystems
{
	/// <summary>
	/// Сервис, ответственный за обучение bpm работе с новыми структурами сообщений.
	/// Отправляет сообщение для обучения, используя rest подход.
	/// Не публикует это сообщение в шину.
	/// Такая степень надежности используется для передачи модели обучения.
	/// Для передачи данных по определенному протоколу, обслуюивающему инциденты - выбрана другая концепция outbox.
	/// </summary>
	public class TeachSenderHandler(
		IHttpClientFactory httpClientFactory,
		ILogger<TeachSenderHandler> logger) : ITeachSenderHandler
	{
		public async Task<ResponseIntegration> TeachBPMAsync(CombinedModel parsedModel, CancellationToken token)
		{
			logger.LogInformation("Начало обработки TeachBPMNAsync");

			var modelForBpmSystem = new InMessage
			{
				InternalModel = parsedModel.InternalModel,
				QueuesNames = new QueuesNames
				{
					InQueueName = parsedModel.InQueueName,
					OutQueueName = parsedModel.OutQueueName
				}
			};

			var client = httpClientFactory.CreateClient();

			try
			{
				// Сериализация объекта в JSON
				var jsonContent = new StringContent(
					JsonConvert.SerializeObject(modelForBpmSystem),
					Encoding.UTF8,
					"application/json"
				);

				logger.LogInformation("Отправка POST-запроса на https://localhost:7054/Integration/save");

				// Отправляем POST-запрос с телом модели для обучения bpm.
				// Для обучения используется rest
				// Для обработки интеграций используется сетевая шина
				// Эти адреса необходимо сделать динамическими
				var response = await client.PostAsync("https://localhost:7054/Integration/save", jsonContent, token);

				if (response.IsSuccessStatusCode)
				{
					logger.LogInformation("Соединение успешно установлено с API, статус-код: {StatusCode}", response.StatusCode);
					return new ResponseIntegration
					{
						Message = "API доступен, соединение успешно установлено.",
						Result = true
					};
				}
				else
				{
					logger.LogWarning("API недоступен, статус-код: {StatusCode}", response.StatusCode);
					return new ResponseIntegration
					{
						Message = $"API недоступен. Статус-код: {(int)response.StatusCode}",
						Result = false
					};
				}
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "Ошибка при обращении к API");
				return new ResponseIntegration
				{
					Message = $"Ошибка при обращении к API: {ex.Message}",
					Result = false
				};
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Неожиданная ошибка при проверке статуса API");
				return new ResponseIntegration
				{
					Message = $"Неожиданная ошибка при проверке статуса API: {ex.Message}",
					Result = false
				};
			}
		}
	}
}
