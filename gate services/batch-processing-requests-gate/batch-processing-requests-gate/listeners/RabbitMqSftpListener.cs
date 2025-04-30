using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using sftp_dynamic_gate_app.models;
using sftp_dynamic_gate_app.services.sftp;

namespace sftp_dynamic_gate_app.listeners
{
	public class RabbitMqSftpListener : IRabbitMqQueueListener<RabbitMqSftpListener>
	{
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<RabbitMqSftpListener> _logger;
		private static readonly ConcurrentDictionary<string, bool> ProcessedFileHashes = new();
		private IConnection _connection;
		private IModel _channel;
		private CancellationTokenSource _cts;
		private string _pathForSaveLocally;
		private readonly ISftpUploader _sftpUploader;

		public RabbitMqSftpListener(
		IConnectionFactory connectionFactory,
		IOptions<SftpSettings> ssftpSettings,
		ILogger<RabbitMqSftpListener> logger,
		ISftpUploader sftpUploader)
		{
			_connectionFactory = connectionFactory;
			_logger = logger;
			_sftpUploader = sftpUploader;
		}

		public async Task StartListeningAsync(
		string queueOutName,
		CancellationToken cancellationToken,
		string pathToPushIn = null,
		Func<string, Task> onMessageReceived = null)
		{
			if (string.IsNullOrWhiteSpace(queueOutName))
			{
				_logger.LogError("Имя очереди не указано или пустое.");
				throw new ArgumentNullException(nameof(queueOutName), "Имя очереди не может быть null или пустым.");
			}

			_pathForSaveLocally = !string.IsNullOrWhiteSpace(pathToPushIn)
				? pathToPushIn
				: @"D:\TempSftp";

			if (!Directory.Exists(_pathForSaveLocally))
			{
				Directory.CreateDirectory(_pathForSaveLocally);
				_logger.LogInformation("Создана директория: {Path}", _pathForSaveLocally);
			}

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_connection = _connectionFactory.CreateConnection();
			_channel = _connection.CreateModel();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += async (model, ea) =>
			{
				var body = ea.Body.ToArray();
				var jsonMessage = Encoding.UTF8.GetString(body);
				var message = JsonConvert.DeserializeObject<FileMessage>(jsonMessage);
				if (message == null || message.FileContent == null || string.IsNullOrWhiteSpace(message.FileName))
				{
					_logger.LogWarning("Сообщение некорректно или неполное. Пропускаем.");
					_channel.BasicNack(ea.DeliveryTag, false, false);
					return;
				}
				var safeFileName = Path.GetFileName(message.FileName?.Trim() ?? "default.txt"); // убираем лишнее и подстраховываемся
				var filePath = Path.Combine(_pathForSaveLocally, safeFileName);

				_logger.LogInformation($"Получен файл: {message.FileName}, размер: {message.FileContent.Length} байт");

				try
				{
					// файл сохраняется на диск:
					await File.WriteAllBytesAsync(filePath, message.FileContent, _cts.Token);

					// файл сохраняется в директорию на сервере:
					await _sftpUploader.UploadAsync(filePath);
					_logger.LogInformation($"Файл сохранён и загружен: {filePath}");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Ошибка при сохранении или загрузке файла.");
				}

				string fileHash = ComputeFileHash(message.FileContent);
				ProcessedFileHashes.TryRemove(fileHash, out _);

				_channel.BasicAck(ea.DeliveryTag, false);
			};

			_channel.BasicConsume(queueOutName, false, consumer);
			_logger.LogInformation($"Начал слушать очередь: {queueOutName}");
			await Task.CompletedTask;
		}

		public void StopListening()
		{
			_logger.LogInformation("Остановка SFTP-слушателя.");
			_cts?.Cancel();
			_channel?.Close();
			_connection?.Close();
		}

		private static string ComputeFileHash(byte[] fileContent)
		{
			using var sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(fileContent);
			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		}
	}
}
