namespace servers_api.models.dynamicgatesettings.parameters
{
	/// <summary>
	/// Класс выполняется, когда происходит запрос.
	/// В HttpContext из конфигурации передаются параметры.
	/// </summary>
	public class ParamsToContextSettings
	{
		private readonly RequestDelegate _next;
		private readonly string _companyName;
		private readonly string _host;
		private readonly int _port;
		private readonly bool _validate;

		// Дополнительные параметры для stream конфигурации:
		private readonly string _protocol;
		private readonly string _dataFormat;
		private readonly string _model;
		private readonly string _dataOptions;
		private readonly string _connectionSettings;

		public ParamsToContextSettings(RequestDelegate next, IConfiguration config)
		{
			_next = next;

			// Общие параметры для обеих конфигураций
			_companyName = config["CompanyName"] ?? "default-company";
			_host = config["Host"] ?? "localhost";
			_port = int.TryParse(config["Port"], out var port) ? port : 5000; // По умолчанию для REST или Stream
			_validate = bool.TryParse(config["Validate"], out var validate) && validate;

			// Параметры для Stream (если они есть)
			_protocol = config["Protocol"] ?? "default";
			_dataFormat = config["DataFormat"] ?? "default";
			_model = config["Model"]?.ToString() ?? string.Empty;
			_dataOptions = config["DataOptions"]?.ToString() ?? string.Empty;
			_connectionSettings = config["ConnectionSettings"]?.ToString() ?? string.Empty;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// Сохраняем параметры в HttpContext.Items
			context.Items["CompanyName"] = _companyName;
			context.Items["Host"] = _host;
			context.Items["Port"] = _port;
			context.Items["Validate"] = _validate;

			// Дополнительные параметры для Stream (если есть)
			if (!string.IsNullOrEmpty(_protocol)) context.Items["Protocol"] = _protocol;
			if (!string.IsNullOrEmpty(_dataFormat)) context.Items["DataFormat"] = _dataFormat;
			if (!string.IsNullOrEmpty(_model)) context.Items["Model"] = _model;
			if (!string.IsNullOrEmpty(_dataOptions)) context.Items["DataOptions"] = _dataOptions;
			if (!string.IsNullOrEmpty(_connectionSettings)) context.Items["ConnectionSettings"] = _connectionSettings;

			// Передаем управление следующему middleware
			await _next(context);
		}
	}
}
