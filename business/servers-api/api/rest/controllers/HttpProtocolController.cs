using Microsoft.AspNetCore.Mvc;
using servers_api.messaging.processing;
using servers_api.models.entities;
using servers_api.models.outbox;
using servers_api.repositories;
using servers_api.validation.headers;


[ApiController]
[Route("[controller]")]
public class HttpProtocolController : ControllerBase
{
	private readonly ILogger<HttpProtocolController> _logger;
	private readonly IHeaderValidationService _headerValidationService;
	private readonly IMessageProcessingService _messageProcessingService;

	/// <summary>
	/// Конструктор
	/// </summary>
	/// <param name="serviceProvider"></param>
	/// <param name="logger"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public HttpProtocolController(
		ILogger<HttpProtocolController> logger,
		IHeaderValidationService headerValidationService,
		IMessageProcessingService messageProcessingService)
	{
		// проверка на зарегистрированность соответствующих сервисов для валидации headers:
		_headerValidationService = headerValidationService;
		_messageProcessingService = messageProcessingService;
		_logger = logger;
	}

	[HttpPost("push")]
	public async Task<IActionResult> PushMessage()
	{
		// Получаем параметры из HttpContext.Items:
		string companyName = HttpContext.Items["CompanyName"]?.ToString() ?? "default-company-name-from-middleware";
		string host = HttpContext.Items["Host"]?.ToString() ?? "localhost";
		string port = HttpContext.Items["Port"]?.ToString() ?? "5000";
		bool validate = (bool?)HttpContext.Items["Validate"] ?? false;

		// Зачитываем, по какому протоколу происходил запрос:
		// Получаем схему запроса (http или https)
		string protocol = Request.Scheme;

		// Формируем названия очередей:
		string queueOut = GetQueueName(companyName, "out");
		string queueIn = GetQueueName(companyName, "in");

		// Читаем сообщение, если получили запрос
		var message = await new StreamReader(Request.Body).ReadToEndAsync();

		// Устанавливаем заголовки для ответа:
		Response.Headers.Append("Content-Type", "text/event-stream");
		Response.Headers.Append("Cache-Control", "no-cache");
		Response.Headers.Append("Connection", "keep-alive");
		Response.Headers.Append("Access-Control-Allow-Origin", "*");

		// Смотрим, валидны ли входящие заголовки:
		var isValid = await _headerValidationService.ValidateHeadersAsync(Request.Headers);

		if (!isValid)
		{
			_logger.LogWarning("Invalid headers.");
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

		return Ok("✅ Валидация прошла, модель отправлена в шину и сохранена в БД.");
	}

	// private methods:
	// Функция для логирования заголовков запроса в консоль.
	private void LogHeaders()
	{
		_logger.LogInformation("Headers Received:");
		foreach (var header in Request.Headers)
		{
			_logger.LogInformation($"  {header.Key}: {header.Value}");
		}
	}
	// Функция для формирования названия очереди:
	private string GetQueueName(string companyName, string postfix)
	{
		// Убираем пробелы,
		// переводим в нижний регистр
		// добавляем постфикс:
		return $"{companyName.Trim().ToLower()}-{postfix}-queue";
	}
}

