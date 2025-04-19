using Newtonsoft.Json.Linq;

namespace servers_api.middleware;

/// <summary>
/// Класс для настройки динамического шлюза на основе расширенной конфигурации.
/// </summary>
public class GateConfiguration
{
	public async Task<(string HttpUrl, string HttpsUrl)> ConfigureDynamicGateAsync(string[] args, WebApplicationBuilder builder)
	{
		var configFilePath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Substring(9) ?? "./configs/teach.json";
		var config = await LoadConfigurationAsync(configFilePath);

		var connection = config["connection"];
		var host = connection?["host"]?.ToString() ?? "localhost";
		var port = int.TryParse(connection?["port"]?.ToString(), out var p) ? p : 5000;

		builder.Configuration["Host"] = host;
		builder.Configuration["Port"] = port.ToString();

		var metadata = config["metadata"];
		if (metadata != null)
		{
			builder.Configuration["Protocol"] = metadata["protocol"]?.ToString() ?? "UNKNOWN";
			builder.Configuration["DataFormat"] = metadata["dataFormat"]?.ToString() ?? "UNKNOWN";
			builder.Configuration["CompanyName"] = metadata["companyName"]?.ToString() ?? "UNKNOWN";
		}

		builder.Configuration["Payload"] = config["payload"].ToString() ?? "UNKNOWN";
		builder.Configuration["TargetUrl"] = config["targetUrl"].ToString() ?? "UNKNOWN";

		var httpUrl = $"http://{host}:80";
		var httpsUrl = $"https://{host}:443";
		return (httpUrl, httpsUrl);
	}

	private static async Task<JObject> LoadConfigurationAsync(string configFilePath)
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

			var json = await File.ReadAllTextAsync(fullPath);
			return JObject.Parse(json);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Ошибка при загрузке конфигурации из файла {configFilePath}: {ex.Message}", ex);
		}
	}
}
