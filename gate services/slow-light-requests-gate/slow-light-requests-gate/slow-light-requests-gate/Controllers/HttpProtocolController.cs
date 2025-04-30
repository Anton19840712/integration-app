using lazy_light_requests_gate.headers;
using lazy_light_requests_gate.processing;
using Microsoft.AspNetCore.Mvc;

namespace lazy_light_requests_gate.Controllers;

[ApiController]
[Route("api/httpprotocol")]
public class HttpProtocolController : ControllerBase
{
	private readonly ILogger<HttpProtocolController> _logger;
	private readonly IHeaderValidationService _headerValidationService;
	private readonly IMessageProcessingService _messageProcessingService;
	private readonly IConfiguration _configuration;

	public HttpProtocolController(
		ILogger<HttpProtocolController> logger,
		IHeaderValidationService headerValidationService,
		IMessageProcessingService messageProcessingService,
		IConfiguration configuration)
	{
		_logger = logger;
		_headerValidationService = headerValidationService;
		_messageProcessingService = messageProcessingService;
		_configuration = configuration;
	}

	[HttpPost("push")]
	public async Task<IActionResult> PushMessage()
	{
		string companyName = _configuration["CompanyName"] ?? "default-company";
		string host = _configuration["Host"] ?? "localhost";
		string port = _configuration["Port"] ?? "5000";
		bool validate = bool.TryParse(_configuration["Validate"], out var v) && v;
		string protocol = Request.Scheme;

		_logger.LogInformation("Параметры шлюза: Company={Company}, Host={Host}, Port={Port}, Validate={Validate}, Protocol={Protocol}",
			companyName, host, port, validate, protocol);

		string queueOut = $"{companyName.Trim().ToLower()}-out";
		string queueIn = $"{companyName.Trim().ToLower()}-in";

		var message = await new StreamReader(Request.Body).ReadToEndAsync();

		Response.Headers.Append("Content-Type", "text/event-stream");
		Response.Headers.Append("Cache-Control", "no-cache");
		Response.Headers.Append("Connection", "keep-alive");
		Response.Headers.Append("Access-Control-Allow-Origin", "*");

		if (validate)
		{
			var isValid = await _headerValidationService.ValidateHeadersAsync(Request.Headers);
			if (!isValid)
			{
				_logger.LogWarning("⚠️ Заголовки не прошли валидацию.");
				return BadRequest("Заголовки не прошли валидацию.");
			}
		}
		else
		{
			_logger.LogInformation("Валидация отключена.");
		}

		LogHeaders();

		await _messageProcessingService.ProcessIncomingMessageAsync(
			message,
			queueOut,
			queueIn,
			host,
			int.Parse(port),
			protocol
		);

		return Ok("✅ Модель отправлена в шину и сохранена в БД.");
	}

	private void LogHeaders()
	{
		_logger.LogInformation("Получены заголовки запроса:");
		foreach (var header in Request.Headers)
		{
			_logger.LogInformation($"  {header.Key}: {header.Value}");
		}
	}
}
