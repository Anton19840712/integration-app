using Newtonsoft.Json.Linq;

namespace lazy_light_requests_gate.middleware;

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
		return await ConfigureRestGate(config, builder);
	}

	private async Task<(string HttpUrl, string HttpsUrl)> ConfigureRestGate(JObject config, WebApplicationBuilder builder)
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
		return await Task.FromResult((httpUrl, httpsUrl));
	}

	private static JObject LoadConfiguration(string configFilePath)
	{
		try
		{
			var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
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
