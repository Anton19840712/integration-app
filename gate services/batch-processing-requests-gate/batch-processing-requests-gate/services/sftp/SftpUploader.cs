using Microsoft.Extensions.Options;
using Renci.SshNet;
using sftp_dynamic_gate_app.models;
using sftp_dynamic_gate_app.services.sftp;

public class SftpUploader : ISftpUploader
{
	private readonly SftpSettings _settings;
	private readonly ILogger<SftpUploader> _logger;

	public SftpUploader(IOptions<SftpSettings> options, ILogger<SftpUploader> logger)
	{
		_settings = options.Value;
		_logger = logger;
	}

	public async Task UploadAsync(string localFilePath, string remoteFileName = null)
	{
		try
		{
			// Оборачиваем SftpClient в using для автоматического вызова Dispose
			using var client = new SftpClient(
				_settings.Host,
				_settings.Port,
				_settings.UserName,
				_settings.Password);

			client.Connect();

			if (!client.IsConnected)
				throw new Exception("Не удалось подключиться к SFTP");

			string fileName = remoteFileName ?? Path.GetFileName(localFilePath);
			string remoteFullPath = _settings.Source.EndsWith("/")
				? _settings.Source + fileName
				: _settings.Source + "/" + fileName;

			// Загружаем файл с локального пути
			await using var fileStream = File.OpenRead(localFilePath);
			client.UploadFile(fileStream, remoteFullPath);

			_logger.LogInformation("Файл '{FileName}' успешно загружен в SFTP в папку '{RemotePath}'", fileName, _settings.Source);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при загрузке файла на SFTP");
			throw;
		}
	}
}
