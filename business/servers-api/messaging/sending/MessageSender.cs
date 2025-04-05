using System.Net.Sockets;
using System.Text;
using servers_api.listenersrabbit;
using servers_api.messaging.formatting;

namespace servers_api.messaging.sending
{
	public class MessageSender : IMessageSender
	{
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<MessageSender> _logger;

		public MessageSender(
			IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener,
			IMessageFormatter messageFormatter,
			ILogger<MessageSender> logger)
		{
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessagesToClientAsync(
			TcpClient client,
			string queueForListening,
			CancellationToken cancellationToken)
		{
			try
			{
				_ = _rabbitMqQueueListener.StartListeningAsync(queueForListening, cancellationToken);

				using var stream = client.GetStream();
				var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

				while (!cancellationToken.IsCancellationRequested && client.Connected)
				{
					// если мы сервер, мы собираем информацию из очереди, но до этого нам было нужно сделать запрос в bpm:
					// TO DO: здесь будет нужно доработать - зачем нам нужно что-то собирать
					// var elements = await _rabbitMqQueueListener.GetCollectedMessagesAsync(cancellationToken);

					//if (elements.Count == 0)
					//{
					//	await Task.Delay(1000, cancellationToken);
					//	continue;
					//}

					//foreach (var message in elements)
					//{
					//	string formattedJson = _messageFormatter.FormatJson(message.Message);
					//	await writer.WriteLineAsync(formattedJson);
					//	_logger.LogInformation("Отправлено клиенту:\n{Json}", formattedJson);
					//	await Task.Delay(2000, cancellationToken);
					//}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning("Ошибка при отправке SSE сообщений: {Message}", ex.Message);
			}
		}
	}
}
