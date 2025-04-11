using servers_api.listenersrabbit;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace servers_api.messaging.sending
{
	public class UdpMessageSender : IConnectionMessageSender
	{
		private readonly UdpClient _udpClient;
		private readonly IPEndPoint _remoteEndPoint;
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<UdpMessageSender> _logger;

		public UdpMessageSender(UdpClient udpClient, IPEndPoint remoteEndPoint, IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener, ILogger<UdpMessageSender> logger)
		{
			_udpClient = udpClient;
			_remoteEndPoint = remoteEndPoint;
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken)
		{
			try
			{
				await _rabbitMqQueueListener.StartListeningAsync(
					queueOutName: queueForListening,
					stoppingToken: cancellationToken,
					onMessageReceived: async message =>
					{
						try
						{
							byte[] data = Encoding.UTF8.GetBytes(message + "\n");
							await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);

							_logger.LogInformation("Сообщение отправлено UDP-клиенту {RemoteEndPoint}: {Message}", _remoteEndPoint, message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка при отправке сообщения UDP-клиенту");
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в процессе отправки сообщений UDP-клиенту");
			}
		}
	}
}
