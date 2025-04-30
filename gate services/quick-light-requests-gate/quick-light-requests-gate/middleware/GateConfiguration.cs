using Newtonsoft.Json.Linq;

namespace middleware;

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

	private async Task<(string HttpUrl, string HttpsUrl)> ConfigureStreamGateAsync(JObject jobjectConfig, WebApplicationBuilder builder)
	{
		var protocol = jobjectConfig["protocol"]?.ToString() ?? "TCP";
		var dataFormat = jobjectConfig["dataFormat"]?.ToString() ?? "json";
		var companyName = jobjectConfig["companyName"]?.ToString() ?? "default-company";
		var model = jobjectConfig["model"]?.ToString() ?? "{}";
		var dataOptions = jobjectConfig["dataOptions"]?.ToString() ?? "{}";
		var connectionSettings = jobjectConfig["connectionSettings"]?.ToString() ?? "{}";

		builder.Configuration["Protocol"] = protocol;
		builder.Configuration["DataFormat"] = dataFormat;
		builder.Configuration["CompanyName"] = companyName;
		builder.Configuration["Model"] = model;
		builder.Configuration["DataOptions"] = dataOptions;
		builder.Configuration["ConnectionSettings"] = connectionSettings;

		var dataOptionsObj = JObject.Parse(dataOptions);
		bool isClient = dataOptionsObj["client"]?.ToObject<bool>() ?? false;
		bool isServer = dataOptionsObj["server"]?.ToObject<bool>() ?? false;

		string host;
		int port;

		if (isServer)
		{
			var serverDetails = dataOptionsObj["serverDetails"];
			host = serverDetails?["host"]?.ToString() ?? "localhost";
			port = int.TryParse(serverDetails?["port"]?.ToString(), out var p) ? p : 6254;

			builder.Configuration["Mode"] = "server";
			builder.Configuration["host"] = host;
			builder.Configuration["port"] = port.ToString();
		}
		else if (isClient)
		{
			var clientDetails = dataOptionsObj["clientDetails"];
			host = clientDetails?["host"]?.ToString() ?? "localhost";
			port = int.TryParse(clientDetails?["port"]?.ToString(), out var p) ? p : 5018;

			// Добавим настройки клиента в конфигурацию
			builder.Configuration["Mode"] = "client";
			builder.Configuration["host"] = host;
			builder.Configuration["port"] = port.ToString();
		}
		else
		{
			throw new InvalidOperationException("Не задан ни client, ни server в dataOptions.");
		}

		var httpUrl = $"http://{host}:80";
		var httpsUrl = $"https://{host}:443";

		return (httpUrl, httpsUrl);
	}

	private static JObject LoadConfiguration(string configFilePath)
	{
		try
		{
			var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
			//var fullPath = Path.GetFullPath(configFilePath, basePath);
			var fullPath = Path.GetFullPath(configFilePath);

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
