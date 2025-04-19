using System.Xml;
using Newtonsoft.Json;
using servers_api.models.dynamicgatesettings.internalusage;

public class JsonParsingService : IJsonParsingService
{
	private readonly ILogger<JsonParsingService> _logger;
	private readonly IConfiguration _configuration;

	public JsonParsingService(ILogger<JsonParsingService> logger, IConfiguration configuration)
	{
		_logger = logger;
		_configuration = configuration;
	}

	public Task<CombinedModel> ParseFromConfigurationAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Чтение конфигурации и создание CombinedModel");

		try
		{
			var protocol = _configuration["Protocol"];
			var dataFormat = _configuration["DataFormat"];
			var companyName = _configuration["CompanyName"];
			var internalModelRaw = _configuration["Payload"];

			if (string.IsNullOrWhiteSpace(protocol) ||
				string.IsNullOrWhiteSpace(dataFormat) ||
				string.IsNullOrWhiteSpace(companyName) ||
				string.IsNullOrWhiteSpace(internalModelRaw))
			{
				_logger.LogWarning("Отсутствуют обязательные поля в конфигурации");
				throw new ArgumentException("Отсутствуют обязательные поля в конфигурации");
			}

			var inQueueName = $"{companyName}_in";
			var outQueueName = $"{companyName}_out";

			var internalModelJson = ConvertModelToJson(internalModelRaw, dataFormat);

			var combinedModel = new CombinedModel
			{
				Protocol = protocol,
				InQueueName = inQueueName,
				OutQueueName = outQueueName,
				InternalModel = internalModelJson,
				DataFormat = dataFormat
			};

			_logger.LogInformation("CombinedModel успешно создан на основе конфигурации");
			return Task.FromResult(combinedModel);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при создании CombinedModel из конфигурации");
			throw new ApplicationException("Ошибка при создании CombinedModel из конфигурации.", ex);
		}
	}

	private static string ConvertModelToJson(string internalModelRaw, string dataFormat)
	{
		if (dataFormat == "xml")
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(internalModelRaw);
			xmlDoc.RemoveChild(xmlDoc.FirstChild); // remove <?xml ... ?>
			return JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.None, true);
		}

		// если формат json
		return internalModelRaw;
	}
}
