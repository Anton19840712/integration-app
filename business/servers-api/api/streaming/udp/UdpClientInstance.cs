using System.Net.Sockets;
using System.Text;
using servers_api.constants;
using servers_api.enums;
using servers_api.factory;
using servers_api.models.entities;
using servers_api.models.internallayer.instance;
using servers_api.models.outbox;
using servers_api.models.response;
using servers_api.repositories;

namespace servers_api.api.streaming.udp
{
	public class UdpClientInstance : IUpClient
	{
		private readonly ILogger<UdpClientInstance> _logger;
		private readonly IMongoRepository<OutboxMessage> _outboxRepository;
		private readonly IMongoRepository<IncidentEntity> _incidentRepository;
		private CancellationTokenSource _cts;
		private string _host;
		private int _port;

		public UdpClientInstance(
			ILogger<UdpClientInstance> logger,
			IMongoRepository<OutboxMessage> outboxRepository,
			IMongoRepository<IncidentEntity> incidentRepository)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
			_incidentRepository = incidentRepository ?? throw new ArgumentNullException(nameof(incidentRepository));
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

					await ProcessMessagesAsync(receivedMessage, instanceModel.InQueueName, instanceModel.OutQueueName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Ошибка UDP-клиента: {Message}", ex.Message);
			}
		}


		private async Task ProcessMessagesAsync(
			string message,
			string instanceModelQueueInName,
			string instanceModelQueueOutName)
		{
			try
			{
				_logger.LogInformation("Логирование сообщения: {Message}", message);

				// Сохранение полученного сообщения в Outbox
				var outboxMessage = new OutboxMessage
				{
					Id = Guid.NewGuid().ToString(),
					ModelType = ModelType.Outbox,
					EventType = EventTypes.Received,
					IsProcessed = false,
					ProcessedAt = DateTime.Now,
					InQueue = instanceModelQueueInName,
					OutQueue = instanceModelQueueOutName,
					Payload = message,
					RoutingKey = "routing_key_udp",
					CreatedAt = DateTime.UtcNow,
					CreatedAtFormatted = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
					Source = $"udp-client-instance based on host: {_host} and port {_port}"
				};

				await _outboxRepository.SaveMessageAsync(outboxMessage);

				// Сохранение инцидента в базу данных
				var incidentEntity = new IncidentEntity
				{
					Payload = message,
					CreatedAtUtc = DateTime.UtcNow,
					CreatedBy = "udp-client-instance",
					IpAddress = "127.0.0.1",
					UserAgent = "udp-client-instance",
					CorrelationId = Guid.NewGuid().ToString(),
					ModelType = "Incident",
					IsProcessed = false
				};

				await _incidentRepository.SaveMessageAsync(incidentEntity);
			}
			catch (Exception ex)
			{
				_logger.LogError("Ошибка при сохранении сообщения в базу данных.");
				_logger.LogError(ex.Message);
			}
		}
	}
}