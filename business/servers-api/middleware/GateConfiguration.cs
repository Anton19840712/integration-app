using System.Text.Json;
using Newtonsoft.Json.Linq;
using servers_api.api.rest.test;
using servers_api.models.dynamicgatesettings.incomingjson;
using servers_api.models.dynamicgatesettings.internalusage;

namespace servers_api.middleware;

/// <summary>
/// Класс используется для предоставления возможности настройщику системы
/// динамически задавать хост и порт самого динамического шлюза.
/// </summary>
public class GateConfiguration
{

	/// <summary>
	/// Настройка динамических параметров шлюза и возврат HTTP/HTTPS адресов
	/// </summary>
	public async Task<(string HttpUrl, string HttpsUrl)> ConfigureDynamicGateAsync(string[] args, WebApplicationBuilder builder)
	{
		var configFilePath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Substring(9) ?? "./configs/stream.json";
		var config = LoadConfiguration(configFilePath);

		var configType = config["type"]?.ToString() ?? config["Type"]?.ToString();
		if (configType == null)
			throw new InvalidOperationException("Тип конфигурации не найден.");

		var configTypeStr = configType.ToLowerInvariant();

		return configTypeStr switch
		{
			"rest" => ConfigureRestGate(config, builder),
			"stream" => await ConfigureStreamGateAsync(config, builder),
			_ => throw new InvalidOperationException($"Неподдерживаемый тип конфигурации: {configTypeStr}")
		};
	}

	private (string HttpUrl, string HttpsUrl) ConfigureRestGate(JObject config, WebApplicationBuilder builder)
	{
		var companyName = config["CompanyName"]?.ToString() ?? "default-company";
		var host = config["Host"]?.ToString() ?? "localhost";
		var port = int.TryParse(config["Port"]?.ToString(), out var p) ? p : 5000;
		var enableValidation = bool.TryParse(config["Validate"]?.ToString(), out var v) && v;

		builder.Configuration["CompanyName"] = companyName;
		builder.Configuration["Host"] = host;
		builder.Configuration["Port"] = port.ToString();
		builder.Configuration["Validate"] = enableValidation.ToString();

		var httpUrl = $"http://{host}:80";
		var httpsUrl = $"https://{host}:443";
		return (httpUrl, httpsUrl);
	}

	private async Task<(string HttpUrl, string HttpsUrl)> ConfigureStreamGateAsync(JObject config, WebApplicationBuilder builder)
	{
		var protocol = config["protocol"]?.ToString() ?? "TCP";
		var dataFormat = config["dataFormat"]?.ToString() ?? "json";
		var companyName = config["companyName"]?.ToString() ?? "default-company";
		var model = config["model"]?.ToString() ?? "{}";
		var dataOptions = config["dataOptions"]?.ToString() ?? "{}";
		var connectionSettings = config["connectionSettings"]?.ToString() ?? "{}";

		builder.Configuration["Protocol"] = protocol;
		builder.Configuration["DataFormat"] = dataFormat;
		builder.Configuration["CompanyName"] = companyName;
		builder.Configuration["Model"] = model;
		builder.Configuration["DataOptions"] = dataOptions;
		builder.Configuration["ConnectionSettings"] = connectionSettings;

		var dataOptionsObj = JObject.Parse(dataOptions);
		var serverDetails = dataOptionsObj["serverDetails"];
		var host = serverDetails?["host"]?.ToString() ?? "localhost";
		var port = int.TryParse(serverDetails?["port"]?.ToString(), out var p) ? p : 6254;

		var httpUrl = $"http://{host}:80";
		var httpsUrl = $"https://{host}:443";

		var combinedModel = CreateCombinedModel(protocol, dataFormat, companyName, model, dataOptions, connectionSettings);

		// 👇 Создаём и запускаем сервер:
		var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
		var cts = new CancellationTokenSource();

		return (httpUrl, httpsUrl);
	}

	private CombinedModel CreateCombinedModel(string protocol, string dataFormat, string company, string model,
		string dataOptionsJson, string connectionSettingsJson)
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		var dataOptions = JsonSerializer.Deserialize<DataOptions>(dataOptionsJson, options);
		var connectionSettings = JsonSerializer.Deserialize<ConnectionSettings>(connectionSettingsJson, options);

		return new CombinedModel
		{
			Id = Guid.NewGuid().ToString(),
			Protocol = protocol,
			DataFormat = dataFormat,
			InternalModel = model,
			InQueueName = $"{company}_in",
			OutQueueName = $"{company}_out",
			DataOptions = dataOptions,
			ConnectionSettings = connectionSettings
		};
	}

	private static JObject LoadConfiguration(string configFilePath)
	{
		try
		{
			var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
			var fullPath = Path.GetFullPath(configFilePath, basePath);
			var fileName = Path.GetFileName(fullPath);

			Console.WriteLine();
			Console.WriteLine($"[INFO] Base path: {basePath}");
			Console.WriteLine($"[INFO] Full config path: {fullPath}");
			Console.WriteLine($"[INFO] Загружается конфигурация: {fileName}");
			Console.WriteLine();

			var json = File.ReadAllText(fullPath);
			return JObject.Parse(json);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Ошибка при загрузке конфигурации из файла {configFilePath}: {ex.Message}");
		}
	}
}
