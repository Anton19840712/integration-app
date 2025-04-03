using servers_api.models.configurationsettings;

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
			var port = GetArgument(args, "--port=", GetConfigValue(builder, "applicationUrl", 5000));
			var host = GetArgument(args, "--host=", "localhost");
			var enableValidation = GetArgument(args, "--validate=", true);
			var companyName = GetArgument(args, "--company=", "default-company");

			// Зафиксируем параметры в конфигурации:
			builder.Configuration["CompanyName"] = companyName;
			builder.Configuration["Host"] = host;
			builder.Configuration["Port"] = port.ToString();
			builder.Configuration["Validate"] = enableValidation.ToString();

			builder.Services.Configure<CompanyMiddlewareSettings>(builder.Configuration);

			// Формируем URL
			var httpsPort = port + 1;
			var httpUrl = $"http://{host}:{port}";
			var httpsUrl = $"https://{host}:{httpsPort}";

			return (httpUrl, httpsUrl);
		}

		/// <summary>
		/// Метод для парсинга аргументов командной строки
		/// </summary>
		private static T GetArgument<T>(string[] args, string key, T defaultValue)
		{
			var arg = args.FirstOrDefault(a => a.StartsWith(key));
			if (arg != null)
			{
				var value = arg.Substring(key.Length);
				try
				{
					return (T)Convert.ChangeType(value, typeof(T));
				}
				catch
				{
					Console.WriteLine($"Ошибка при разборе аргумента {key}. Используется значение по умолчанию: {defaultValue}");
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Метод для получения значения из конфигурации (например, из launchSettings.json)
		/// </summary>
		private static T GetConfigValue<T>(WebApplicationBuilder builder, string key, T defaultValue)
		{
			var configValue = builder.Configuration[key];
			if (configValue != null)
			{
				try
				{
					return (T)Convert.ChangeType(configValue, typeof(T));
				}
				catch
				{
					Console.WriteLine($"Ошибка при разборе конфигурации по ключу {key}. Используется значение по умолчанию: {defaultValue}");
				}
			}
			return defaultValue;
		}
	}
}
