using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BPMMessaging.models.dtos;
using BPMMessaging.models.entities;
using System.Text.Json;
using BPMMessaging.enums;
using BPMMessaging.repository;
using MongoDB.Bson;

public class RabbitMqQueueListener
{
	private readonly ILogger<RabbitMqQueueListener> _logger;
	private readonly IConnectionFactory _connectionFactory;
	private readonly IMongoRepository<IncidentEntity> _incidentRepository;
	private readonly IMongoRepository<OutboxMessage> _outboxRepository;

	private IConnection _connection;
	private IModel _channel;
	private string _queueInName;
	private string _queueOutName;

	public RabbitMqQueueListener(
		IConnectionFactory connectionFactory,
		ILogger<RabbitMqQueueListener> logger,
		IMongoRepository<IncidentEntity> incidentRepository,
		IMongoRepository<OutboxMessage> outboxRepository)
	{
		_connectionFactory = connectionFactory;
		_logger = logger;
		_incidentRepository = incidentRepository;
		_outboxRepository = outboxRepository;
	}

	public async Task StartListeningAsync(
		string queueInName,
		string queueOutName,
		CancellationToken stoppingToken)
	{
		_queueInName = queueInName;
		_queueOutName = queueOutName;
		_connection = _connectionFactory.CreateConnection();
		_channel = _connection.CreateModel();

		while (!QueueExists(_channel, _queueInName))
		{
			_logger.LogWarning("Очередь {Queue} еще не создана. Ожидание...", _queueInName);
			await Task.Delay(1000, stoppingToken);
		}

		var consumer = new EventingBasicConsumer(_channel);
		consumer.Received += async (model, ea) => await HandleMessageAsync(ea);

		_channel.BasicConsume(queue: _queueInName, autoAck: true, consumer: consumer);
		_logger.LogInformation("Слушатель очереди {Queue} запущен", _queueInName);
	}

	private bool QueueExists(IModel channel, string queueName)
	{
		try
		{
			channel.QueueDeclarePassive(queueName);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
	{
		var message = Encoding.UTF8.GetString(ea.Body.ToArray());
		_logger.LogInformation("Получено сообщение из очереди {Queue}: {Message}", _queueInName, message);

		// Сохраняем в MongoDB базу IncidentDB как incident:
		var incident = new IncidentEntity
		{
			ModelType = "incident",
			InQueueName = _queueInName,
			OutQueueName = _queueOutName,
			IncidentData = message
		};
		await _incidentRepository.InsertAsync(incident);
		_logger.LogInformation("Сообщение сохранено в таблицу инцидентов");

		// Сохраняем в MongoDB базу IncidentDB как outbox:
		var outboxMessage = new OutboxMessage
		{
			ModelType = "outbox",
			EventType = EventTypes.Received,
			IsProcessed = false,
			OutQueue = _queueOutName,
			InQueue = _queueInName,
			Payload = BsonDocument.Parse(JsonSerializer.Serialize(incident))
		};
		await _outboxRepository.InsertAsync(outboxMessage);
		_logger.LogInformation("Сообщение сохранено в таблицу Outbox");
	}

	public void StopListening()
	{
		_channel?.Close();
		_connection?.Close();
		_logger.LogInformation("Слушатель {Queue} остановлен", _queueInName);
	}
}
