using System.Text.Json;
using Newtonsoft.Json;
using System.Xml;
using servers_api.Services.Parsers;
using servers_api.models.configurationsettings;
using servers_api.models.internallayer.common;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;

public class JsonParsingService : IJsonParsingService
{
	private readonly ILogger<JsonParsingService> _logger;

	public JsonParsingService(ILogger<JsonParsingService> logger)
	{
		_logger = logger;
	}

	public Task<CombinedModel> ParseJsonAsync(JsonElement jsonBody, bool isTeaching, CancellationToken stoppingToken)
	{
		_logger.LogInformation("Начало разбора JSON");

		try
		{
			if (!AreRequiredFieldsPresent(jsonBody, isTeaching))
			{
				_logger.LogWarning("Пропущены обязательные поля JSON");
				throw new ArgumentException("Пропущены обязательные поля JSON");
			}

			var protocol = jsonBody.GetProperty("protocol").GetString();
			var dataFormat = jsonBody.GetProperty("dataFormat").GetString();
			var companyName = jsonBody.GetProperty("companyName").GetString();
			var inQueueName = $"{companyName}_in";
			var outQueueName = $"{companyName}_out";

			string jsonString = ConvertXmlToJson(jsonBody, dataFormat, isTeaching);

			ConnectionSettings connectionSettings = null;
			DataOptions dataOptions = null;

			if (!isTeaching)
			{
				dataOptions = Deserialize<DataOptions>(jsonBody.GetProperty("dataOptions").GetRawText());
				connectionSettings = DeserializeConnectionSettings(jsonBody.GetProperty("connectionSettings"));
			}

			var combinedModel = new CombinedModel
			{
				Protocol = protocol,
				InQueueName = inQueueName,
				OutQueueName = outQueueName,
				InternalModel = jsonString,
				DataFormat = dataFormat,
				DataOptions = dataOptions,
				ConnectionSettings = connectionSettings
			};

			_logger.LogInformation("JSON успешно разобран и преобразован в CombinedModel");
			return Task.FromResult(combinedModel);
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "Ошибка при обработке JSON: неверный формат данных");
			throw new ArgumentException("Ошибка при обработке JSON: неверный формат данных.", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Произошла ошибка при разборе JSON");
			throw new ApplicationException("Произошла ошибка при разборе JSON.", ex);
		}
	}

	private static bool AreRequiredFieldsPresent(JsonElement jsonBody, bool isIntegration)
	{
		return jsonBody.TryGetProperty("protocol", out _) &&
			   jsonBody.TryGetProperty("dataFormat", out _) &&
			   jsonBody.TryGetProperty("companyName", out _) &&
			   (isIntegration || jsonBody.TryGetProperty("dataOptions", out _) && jsonBody.TryGetProperty("connectionSettings", out _));
	}

	private static string ConvertXmlToJson(JsonElement jsonBody, string dataFormat, bool isIntegration)
	{
		if (dataFormat != "xml" && isIntegration)
		{
			return jsonBody.GetProperty("model").ToString();
		}
		if (dataFormat == "xml")
		{
			var xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(jsonBody.GetProperty("model").ToString());
			xmlDocument.RemoveChild(xmlDocument.FirstChild);
			return JsonConvert.SerializeXmlNode(xmlDocument, Newtonsoft.Json.Formatting.None, true);
		}

		return string.Empty;
	}

	private static ConnectionSettings DeserializeConnectionSettings(JsonElement connectionSettingsElement)
	{
		return new ConnectionSettings
		{
			ClientConnectionSettings = Deserialize<ClientSettings>(connectionSettingsElement.GetProperty("clientSettings").GetRawText()),
			ServerConnectionSettings = Deserialize<ServerSettings>(connectionSettingsElement.GetProperty("serverSettings").GetRawText())
		};
	}

	private static T Deserialize<T>(string json)
	{
		return JsonSerializer.Deserialize<T>(json);
	}
}
