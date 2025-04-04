using System.Net.Sockets;
using System.Text;
using servers_api.constants;
using servers_api.factory;
using servers_api.messaging.processing;
using servers_api.models.internallayer.instance;
using servers_api.models.response;


namespace servers_api.api.streaming.tcp
{
	public class TcpClientInstance : IUpClient
	{
		private readonly ILogger<TcpClientInstance> _logger;
		private readonly IMessageProcessingService _messageProcessingService;
		private CancellationTokenSource _cts;
		private string _host;
		private int _port;

		public TcpClientInstance(
			ILogger<TcpClientInstance> logger,
			IMessageProcessingService messageProcessingService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_messageProcessingService = messageProcessingService;

		}

		public Task<ResponseIntegration> ConnectToServerAsync(
			ClientInstanceModel instanceModel,
			string serverHost,
			int serverPort,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation("Запуск TCP-клиента...");
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			_ = ConnectToServerThisAsync(instanceModel, serverHost, serverPort, _cts.Token);

			return Task.FromResult(new ResponseIntegration()
			{
				Message = "Успешное подключение.",
				Result = true
			});
		}

		private async Task ConnectToServerThisAsync(
			ClientInstanceModel instanceModel,
			string serverHost,
			int serverPort,
			CancellationToken token)
		{
			_host = string.IsNullOrWhiteSpace(serverHost) ? ProtocolClientConstants.DefaultServerHost : serverHost;
			_port = serverPort == 0 ? ProtocolClientConstants.DefaultServerPort : serverPort;

			while (!token.IsCancellationRequested)
			{
				using var client = new TcpClient();

				try
				{
					_logger.LogInformation("Подключение к {Host}:{Port}...", _host, _port);
					await client.ConnectAsync(_host, _port);
					_logger.LogInformation("Успешное подключение!");

					using var stream = client.GetStream();
					byte[] buffer = new byte[ProtocolClientConstants.BufferSize];

					while (!token.IsCancellationRequested)
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
						if (bytesRead == 0)
						{
							_logger.LogWarning("Сервер закрыл соединение.");
							break;
						}

						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						_logger.LogInformation("Получено: {Message}", message);

						_logger.LogInformation("Логирование сообщения: {Message}", message);
						await _messageProcessingService.ProcessIncomingMessageAsync(
							message: message,
							instanceModelQueueInName: instanceModel.InQueueName,
							instanceModelQueueOutName: instanceModel.OutQueueName,
							host: _host,
							port: _port,
							protocol: "tcp");
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Ошибка TCP-клиента: {Message}", ex.Message);
				}
			}
		}
	}
}