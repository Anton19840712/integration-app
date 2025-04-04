using System.Net.Sockets;
using System.Text;
using servers_api.constants;
using servers_api.factory;
using servers_api.messaging.processing;
using servers_api.models.internallayer.instance;
using servers_api.models.response;

namespace servers_api.api.streaming.udp
{
	public class UdpClientInstance : IUpClient
	{
		private readonly ILogger<UdpClientInstance> _logger;
		private readonly IMessageProcessingService _messageProcessingService;
		private CancellationTokenSource _cts;
		private string _host;
		private int _port;

		public UdpClientInstance(
			ILogger<UdpClientInstance> logger,
			IMessageProcessingService messageProcessingService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public Task<ResponseIntegration> ConnectToServerAsync(
			ClientInstanceModel instanceModel,
			string serverHost,
			int serverPort,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation("Запуск UDP-клиента...");
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

			using var client = new UdpClient();

			try
			{
				_logger.LogInformation("Запуск UDP-клиента, отправка пакетов на {Host}:{Port}...", _host, _port);
				client.Connect(_host, _port); // Можно вызывать, но не обязательно

				while (!token.IsCancellationRequested)
				{
					// Отправка сообщения серверу для его понимания, что мы существуюем.
					string messageToSend = "Hello from UDP client";
					byte[] sendBytes = Encoding.UTF8.GetBytes(messageToSend);
					await client.SendAsync(sendBytes, sendBytes.Length);

					_logger.LogInformation("Отправлено health-check сообщение с клиента udp: {Message}", messageToSend);

					// Ожидание ответа от сервера
					UdpReceiveResult receivedResult = await client.ReceiveAsync();
					string receivedMessage = Encoding.UTF8.GetString(receivedResult.Buffer);

					_logger.LogInformation("Получено сообщение с адреса {RemoteEndPoint}: {Message}",
						receivedResult.RemoteEndPoint, receivedMessage);

					await _messageProcessingService.ProcessIncomingMessageAsync(
							message: receivedMessage,
							instanceModelQueueInName: instanceModel.InQueueName,
							instanceModelQueueOutName: instanceModel.OutQueueName,
							host: _host,
							port: _port,
							protocol: "udp");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Ошибка UDP-клиента: {Message}", ex.Message);
			}
		}
	}
}
