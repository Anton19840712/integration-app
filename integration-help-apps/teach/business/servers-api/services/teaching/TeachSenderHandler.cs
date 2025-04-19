using System.Text;
using Newtonsoft.Json;
using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.models.queues;
using servers_api.models.response;

namespace servers_api.services.teaching
{
	public class TeachSenderHandler(
		IConfiguration configuration,
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
			int maxRetries = 3;
			int delayMs = 1000; // задержка между попытками
			int attempt = 0;

			while (attempt < maxRetries)
			{
				try
				{
					attempt++;

					var jsonContent = new StringContent(
						JsonConvert.SerializeObject(modelForBpmSystem),
						Encoding.UTF8,
						"application/json"
					);

					string url = configuration["targetUrl"].ToString();
					logger.LogInformation("TeachServiceHandler: попытка {Attempt}/{MaxRetries}: отправка POST-запроса на {Url}", attempt, maxRetries, url);

					var response = await client.PostAsync(url, jsonContent, token);

					if (response.IsSuccessStatusCode)
					{
						logger.LogInformation("TeachServiceHandler: соединение успешно установлено с API, статус-код: {StatusCode}", response.StatusCode);
						return new ResponseIntegration
						{
							Message = "API доступен, соединение успешно установлено.",
							Result = true
						};
					}
					else
					{
						logger.LogWarning("TeachServiceHandler: API недоступен, статус-код: {StatusCode}", response.StatusCode);
						return new ResponseIntegration
						{
							Message = $"API недоступен. Статус-код: {(int)response.StatusCode}",
							Result = false
						};
					}
				}
				catch (HttpRequestException ex)
				{
					logger.LogWarning("TeachServiceHandler: попытка {Attempt}/{MaxRetries} не удалась: {Message}", attempt, maxRetries, ex.Message);
					if (attempt >= maxRetries)
					{
						logger.LogError("Превышено количество попыток соединения с API. Ошибка: {Message}", ex.Message);

						return new ResponseIntegration
						{
							Message = $"Ошибка при обращении к API: {ex.Message}",
							Result = false
						};
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Неожиданная ошибка при обращении к API.");
					return new ResponseIntegration
					{
						Message = $"Неожиданная ошибка при проверке статуса API: {ex.Message}",
						Result = false
					};
				}

				await Task.Delay(delayMs); // Пауза перед новой попыткой
			}

			return new ResponseIntegration
			{
				Message = "Успешная отправка модели в bpme",
				Result = true
			};
		}
	}
}
