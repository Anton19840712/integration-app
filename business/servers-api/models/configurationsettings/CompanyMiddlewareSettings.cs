namespace servers_api.models.configurationsettings
{
	/// <summary>
	/// Класс выполнится, когда произойдет запрос.
	/// В http context из конфигурации будут переданы параметры.
	/// </summary>
	public class CompanyMiddlewareSettings
	{
		private readonly RequestDelegate _next;
		private readonly string _companyName;
		private readonly string _host;
		private readonly string _port;
		private readonly bool _validate;

		public CompanyMiddlewareSettings(RequestDelegate next, IConfiguration config)
		{
			// страхуемся, если в конфигурацию что-то не установилось:
			_next = next;
			_companyName = config["CompanyName"] ?? "default-middleware-from-company-middleware-settings";
			_host = config["Host"] ?? "localhost";
			_port = config["Port"] ?? "5000";
			_validate = bool.TryParse(config["Validate"], out var validate) && validate;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// сохраняем параметры в HttpContext.Items:
			context.Items["CompanyName"] = _companyName;
			context.Items["Host"] = _host;
			context.Items["Port"] = _port;
			context.Items["Validate"] = _validate;

			await _next(context);
		}
	}
}
