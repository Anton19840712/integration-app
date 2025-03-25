using System.Text;
using AutoMapper;
using BPMMessaging.models.dtos;
using BPMMessaging.models.settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace BPMMessaging.publishing
{
	public class MessagePublisher : IMessagePublisher
	{
		private readonly ILogger<MessagePublisher> _logger;
		private readonly ConnectionFactory _connectionFactory;
		private readonly IMapper _mapper;

		public MessagePublisher(
			IMapper mapper,
			ILogger<MessagePublisher> logger,
			IOptions<RabbitMqSettings> rabbitMqOptions)
		{
			_mapper = mapper;
			_logger = logger;
			var rabbitMqSettings = rabbitMqOptions.Value;

			_connectionFactory = new ConnectionFactory
			{
				HostName = rabbitMqSettings.HostName,
				Port = rabbitMqSettings.Port,
				UserName = rabbitMqSettings.UserName,
				Password = rabbitMqSettings.Password
			};
		}

		public async Task PublishAsync(
			string queueName,
			OutboxMessage payload,
			CancellationToken stoppingToken)
		{
			try
			{
				await Task.Run(() =>
				{
					using var connection = _connectionFactory.CreateConnection();
					using var channel = connection.CreateModel();

					// Сообщение теперь тоже персистентное
					var properties = channel.CreateBasicProperties();
					properties.Persistent = true;

					// Очередь теперь постоянная
					channel.QueueDeclare(
						queue: queueName,
						durable: true,
						exclusive: false,
						autoDelete: false,
						arguments: null);

					OutModel outModel = _mapper.Map<OutModel>(payload);

					// Теперь сериализуем payload
					var jsonString = JsonConvert.SerializeObject(outModel);

					var body = Encoding.UTF8.GetBytes(jsonString);

					channel.BasicPublish(
						exchange: "",
						routingKey: queueName,
						basicProperties: properties,
						body: body
					);

					_logger.LogInformation($"Сообщение опубликовано в очередь {queueName}: {jsonString}");
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при публикации сообщения в очередь {QueueName}: {Payload}", queueName, payload);
			}
		}
	}
}
