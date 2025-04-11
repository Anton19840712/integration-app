using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using servers_api.models.configurationsettings;
using System.Collections.Concurrent;
using System.Security.Cryptography;


[ApiController]
[Route("api/sftp")]
public class SftpProtocolController : ControllerBase
{
	private readonly IRabbitMqService _rabbitMqService;
	private readonly ILogger<SftpProtocolController> _logger;
	private readonly FileHashService _fileHashService;
	public SftpProtocolController(
		IRabbitMqService rabbitMqService,
		ILogger<SftpProtocolController> logger,
		FileHashService fileHashService,
		IOptions<SftpSettings> sftpSettings)
	{
		_rabbitMqService = rabbitMqService;
		_logger = logger;
		_fileHashService = fileHashService;
	}

	[HttpPost("upload/{queueName}")]
	public async Task<IActionResult> UploadFile(IFormFile file, string queueName)
	{
		try
		{
			// Проверяем, что название очереди не пустое
			if (string.IsNullOrWhiteSpace(queueName))
			{
				return BadRequest("Название очереди не может быть пустым.");
			}

			// Копируем содержимое файла в память
			using var stream = new MemoryStream();
			await file.CopyToAsync(stream);
			byte[] fileContent = stream.ToArray();

			// Вычисляем хеш файла
			string fileHash = ComputeFileHash(fileContent);

			// Проверяем, был ли этот файл уже загружен
			if (!_fileHashService.TryAddHash(fileHash))
			{
				_logger.LogInformation("Файл уже был загружен: {FileName}", file.FileName);
				return BadRequest("Этот файл уже был загружен.");
			}

			// Получаем расширение файла
			string fileExtension = Path.GetExtension(file.FileName);

			// Создаем объект сообщения для отправки в очередь
			var message = new
			{
				FileName = file.FileName,           // Добавляем имя файла
				FileContent = Convert.ToBase64String(fileContent), // Кодируем в base64
			};

			string jsonMessage = JsonConvert.SerializeObject(message);
			await _rabbitMqService.PublishMessageAsync(queueName, queueName, jsonMessage);

			// Возвращаем успешный ответ
			return Ok($"Файл успешно загружен и передан в очередь '{queueName}'.");
		}
		catch (Exception ex)
		{
			// Логируем ошибку и возвращаем статус 500
			_logger.LogError(ex, "Ошибка при загрузке файла.");
			return StatusCode(500, "Произошла ошибка при обработке файла.");
		}
	}
	private static string ComputeFileHash(byte[] fileContent)
	{
		using var sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(fileContent);
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
	}
}

public class FileHashService
{
	private readonly ConcurrentDictionary<string, bool> _processedFileHashes = new();

	public bool TryAddHash(string fileHash)
	{
		return _processedFileHashes.TryAdd(fileHash, true);
	}

	public bool RemoveHash(string fileHash)
	{
		return _processedFileHashes.TryRemove(fileHash, out _);
	}

	public bool ContainsHash(string fileHash)
	{
		return _processedFileHashes.ContainsKey(fileHash);
	}
}