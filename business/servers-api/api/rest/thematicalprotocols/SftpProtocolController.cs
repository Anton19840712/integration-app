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
			// ���������, ��� �������� ������� �� ������
			if (string.IsNullOrWhiteSpace(queueName))
			{
				return BadRequest("�������� ������� �� ����� ���� ������.");
			}

			// �������� ���������� ����� � ������
			using var stream = new MemoryStream();
			await file.CopyToAsync(stream);
			byte[] fileContent = stream.ToArray();

			// ��������� ��� �����
			string fileHash = ComputeFileHash(fileContent);

			// ���������, ��� �� ���� ���� ��� ��������
			if (!_fileHashService.TryAddHash(fileHash))
			{
				_logger.LogInformation("���� ��� ��� ��������: {FileName}", file.FileName);
				return BadRequest("���� ���� ��� ��� ��������.");
			}

			// �������� ���������� �����
			string fileExtension = Path.GetExtension(file.FileName);

			// ������� ������ ��������� ��� �������� � �������
			var message = new
			{
				FileName = file.FileName,           // ��������� ��� �����
				FileContent = Convert.ToBase64String(fileContent), // �������� � base64
			};

			string jsonMessage = JsonConvert.SerializeObject(message);
			await _rabbitMqService.PublishMessageAsync(queueName, queueName, jsonMessage);

			// ���������� �������� �����
			return Ok($"���� ������� �������� � ������� � ������� '{queueName}'.");
		}
		catch (Exception ex)
		{
			// �������� ������ � ���������� ������ 500
			_logger.LogError(ex, "������ ��� �������� �����.");
			return StatusCode(500, "��������� ������ ��� ��������� �����.");
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