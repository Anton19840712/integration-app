using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Renci.SshNet;
using servers_api.models.configurationsettings;
using servers_api.models.queues;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

namespace servers_api.listenersrabbit
{
	public class RabbitMqSftpListener : IRabbitMqQueueListener<RabbitMqSftpListener>
	{
		private readonly SftpSettings _sftpSettings;
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<RabbitMqSftpListener> _logger;
		private static readonly ConcurrentDictionary<string, bool> ProcessedFileHashes = new();
		private IConnection _connection;
		private IModel _channel;
		private CancellationTokenSource _cts;
		private string _pathForSave;
		public RabbitMqSftpListener(
			IConnectionFactory connectionFactory,
			IOptions<SftpSettings> ssftpSettings,
			ILogger<RabbitMqSftpListener> logger)
		{
			_connectionFactory = connectionFactory;
			_sftpSettings = ssftpSettings.Value;
			_logger = logger;
		}

		public async Task StartListeningAsync(
			string queueOutName,
			CancellationToken cancellationToken,
			string pathToPushIn = null, Func<string, Task> onMessageReceived = null)
		{
			_pathForSave = pathToPushIn;
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			_connection = _connectionFactory.CreateConnection();
			_channel = _connection.CreateModel();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += async (model, ea) =>
			{
				var body = ea.Body.ToArray();
				var jsonMessage = Encoding.UTF8.GetString(body);
				var message = JsonConvert.DeserializeObject<FileMessage>(jsonMessage);
				_logger.LogInformation($"FileName: {message.FileName}");

				byte[] fileContent = message.FileContent;
				string fileName = message.FileName;
				var filePath = Path.Combine(_pathForSave, fileName);

				await File.WriteAllBytesAsync(filePath, fileContent, _cts.Token);

				UploadFileToSftp(filePath);
				_logger.LogInformation($"Файл сохранён: {filePath}");

				string fileHash = ComputeFileHash(fileContent);
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
		private void UploadFileToSftp(string filePath)
		{
			try
			{
				using (var sftpClient = new SftpClient(
					_sftpSettings.Host,
					_sftpSettings.Port,
					_sftpSettings.UserName,
					_sftpSettings.Password))
				{
					sftpClient.Connect();

					var fileName = Path.GetFileName(filePath);
					var remoteFilePath = fileName;

					using (var fileStream = File.OpenRead(filePath))
					{
						sftpClient.UploadFile(fileStream, remoteFilePath);
						_logger.LogInformation("Файл '{FileName}' успешно загружен в '{RemoteFilePath}'", fileName, remoteFilePath);
					}

					sftpClient.Disconnect();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при загрузке файла на SFTP.");
			}
		}

	}
}
