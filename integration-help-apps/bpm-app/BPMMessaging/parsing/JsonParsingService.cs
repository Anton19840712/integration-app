using BPMMessaging.models.entities;
using BPMMessaging.parsing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

public class JsonParsingService : IJsonParsingService
{
	private readonly ILogger<JsonParsingService> _logger;

	public JsonParsingService(ILogger<JsonParsingService> logger)
	{
		_logger = logger;
	}

	public T ParseJson<T>(JsonElement jsonBody) where T : class
	{
		try
		{
			_logger.LogInformation("Начало парсинга JSON в {ModelType}", typeof(T).Name);

			if (!jsonBody.TryGetProperty("QueuesNames", out var queuesNames) ||
				!queuesNames.TryGetProperty("InQueueName", out var inQueueName) ||
				!queuesNames.TryGetProperty("OutQueueName", out var outQueueName) ||
				!jsonBody.TryGetProperty("InternalModel", out var internalModel))
			{
				_logger.LogError("Отсутствуют обязательные поля.");
				throw new ArgumentException("Пропущены необходимые поля JSON");
			}

			var internalModelString = internalModel.GetString();
			var parsedObject = JsonConvert.DeserializeObject<JObject>(internalModelString);

			if (typeof(T) == typeof(TeachingEntity))
			{
				return new TeachingEntity
				{
					InQueueName = inQueueName.GetString(),
					OutQueueName = outQueueName.GetString(),
					IncomingModel = BsonDocument.Parse(parsedObject.ToString(Formatting.None))
				} as T;
			}

			if (typeof(T) == typeof(IncidentEntity))
			{
				return new IncidentEntity
				{
					InQueueName = inQueueName.GetString(),
					OutQueueName = outQueueName.GetString(),
					IncidentData = parsedObject.ToString()
				} as T;
			}

			throw new InvalidOperationException($"Неизвестный тип модели: {typeof(T).Name}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при парсинге JSON.");
			throw;
		}
	}
}
