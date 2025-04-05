using Newtonsoft.Json.Linq;

namespace servers_api.middleware
{
	/// <summary>
	/// Класс используется для предоставления возможности настройщику системы
	/// динамически задавать хост и порт самого динамического шлюза.
	/// </summary>
	public static class GateConfiguration
	{
		/// <summary>
		/// Настройка динамических параметров шлюза и возврат HTTP/HTTPS адресов
		/// </summary>
		public static (string HttpUrl, string HttpsUrl) ConfigureDynamicGate(string[] args, WebApplicationBuilder builder)
		{
			var configFilePath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Substring(9) ?? "./configs/default.json";
			var config = LoadConfiguration(configFilePath);

			var configType = config["type"]?.ToString()?.ToLowerInvariant();
			return configType switch
			{
				"rest" => ConfigureRestGate(config, builder),
				"stream" => ConfigureStreamGate(config, builder),
				_ => throw new InvalidOperationException($"Неподдерживаемый тип конфигурации: {configType}")
			};
		}

		/// <summary>
		/// Настройка для конфигурации типа rest
		/// </summary>
		private static (string HttpUrl, string HttpsUrl) ConfigureRestGate(JObject config, WebApplicationBuilder builder)
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

		/// <summary>
		/// Настройка для конфигурации типа stream
		/// </summary>
		private static (string HttpUrl, string HttpsUrl) ConfigureStreamGate(JObject config, WebApplicationBuilder builder)
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
			return (httpUrl, httpsUrl);
		}

		/// <summary>
		/// Метод для загрузки конфигурации из JSON файла
		/// </summary>
		private static JObject LoadConfiguration(string configFilePath)
		{
			try
			{
				var json = File.ReadAllText(configFilePath);
				return JObject.Parse(json);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Ошибка при загрузке конфигурации из файла {configFilePath}: {ex.Message}");
			}
		}
	}
}
