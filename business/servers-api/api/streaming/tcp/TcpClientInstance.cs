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


namespace servers_api.api.streaming.tcp
{
	public class TcpClientInstance : IUpClient
	{
		private readonly ILogger<TcpClientInstance> _logger;
		private readonly IMongoRepository<OutboxMessage> _outboxRepository;
		private readonly IMongoRepository<IncidentEntity> _incidentRepository;
		private CancellationTokenSource _cts;
		private string _host;
		private int _port;

		public TcpClientInstance(
			ILogger<TcpClientInstance> logger,
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

						await ProcessMessagesAsync(
							message,
							instanceModel.InQueueName,
							instanceModel.OutQueueName);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Ошибка TCP-клиента: {Message}", ex.Message);
				}
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
					RoutingKey = "routing_key_tcp",
					CreatedAt = DateTime.UtcNow,
					CreatedAtFormatted = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
					Source = $"tcp-client-instance based on host: {_host} and port {_port}"
				};

				await _outboxRepository.SaveMessageAsync(outboxMessage);

				// Сохранение инцидента в базу данных
				var incidentEntity = new IncidentEntity
				{
					Payload = message,
					CreatedAtUtc = DateTime.UtcNow,
					CreatedBy = "tcp-client-instance",
					IpAddress = "127.0.0.1",
					UserAgent = "tcp-client-instance",
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