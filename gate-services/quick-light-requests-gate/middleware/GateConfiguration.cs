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

		return await ConfigureStreamGateAsync(config, builder);
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

		int httpPort = int.TryParse(jobjectConfig["httpPort"]?.ToString(), out var hPort) ? hPort : -1;
		int httpsPort = int.TryParse(jobjectConfig["httpsPort"]?.ToString(), out var sPort) ? sPort : -1;

		var urls = new List<string>();

		string httpUrl = string.Empty, httpsUrl = string.Empty;

		if (httpPort > 0)
		{
			httpUrl = $"http://{host}:{httpPort}";
			urls.Add(httpUrl);
		}

		if (httpsPort > 0)
		{
			httpsUrl = $"https://{host}:{httpsPort}";
			urls.Add(httpsUrl);
		}

		if (httpPort <= 0 && httpsPort <= 0)
		{
			httpUrl = $"http://{host}:80";
			httpsUrl = $"https://{host}:443";
			urls.Add(httpUrl);
			urls.Add(httpsUrl);
		}

		return (httpUrl, httpsUrl);
	}

	private static JObject LoadConfiguration(string configFilePath)
	{
		try
		{
			string fullPath;

			if (Path.IsPathRooted(configFilePath))
			{
				fullPath = configFilePath;
			}
			else
			{
				var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
				fullPath = Path.GetFullPath(Path.Combine(basePath, configFilePath));
			}

			// Печатаем информацию для отладки.
			Console.WriteLine();
			Console.WriteLine($"[INFO] Конечный путь к конфигу: {fullPath}");
			Console.WriteLine($"[INFO] Загружается конфигурация: {Path.GetFileName(fullPath)}");
			Console.WriteLine();

			// Проверка существования файла.
			if (!File.Exists(fullPath))
				throw new FileNotFoundException("Файл конфигурации не найден", fullPath);

			// Загружаем конфигурацию из файла.
			var json = File.ReadAllText(fullPath);
			return JObject.Parse(json);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Ошибка при загрузке конфигурации из файла '{configFilePath}': {ex.Message}");
		}
	}
}
