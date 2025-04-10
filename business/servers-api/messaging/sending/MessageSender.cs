using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Connections;
using servers_api.api.rest.test;
using servers_api.listenersrabbit;

namespace servers_api.messaging.sending
{
	public class MessageSender : IMessageSender
	{
		private readonly IRabbitMqQueueListener<RabbitMqQueueListener> _rabbitMqQueueListener;
		private readonly ILogger<MessageSender> _logger;

		public MessageSender(
			IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener,
			ILogger<MessageSender> logger)
		{
			_rabbitMqQueueListener = rabbitMqQueueListener;
			_logger = logger;
		}

		public async Task SendMessagesToClientAsync(
			IConnectionContext connectionContext,
			string queueForListening,
			CancellationToken cancellationToken)
		{
			switch (connectionContext)
			{
				case TcpConnectionContext tcpContext:
					await SendViaTcpAsync(tcpContext.TcpClient, queueForListening, cancellationToken);
					break;

				case UdpConnectionContext udpContext:
					await SendViaUdpAsync(udpContext.UdpClient, udpContext.RemoteEndPoint, queueForListening, cancellationToken);
					break;

				default:
					throw new NotSupportedException("Unsupported connection type.");
			}
		}

		private async Task SendViaTcpAsync(
			TcpClient tcpClient,
			string queueForListening,
			CancellationToken cancellationToken)
		{
			try
			{
				var stream = tcpClient.GetStream();

				await _rabbitMqQueueListener.StartListeningAsync(
					queueOutName: queueForListening,
					stoppingToken: cancellationToken,
					onMessageReceived: async message =>
					{
						try
						{
							if (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Client.Available == 0)
							{
								_logger.LogWarning("TCP-клиент отключился (низкоуровневая проверка).");
								return;
							}

							byte[] data = Encoding.UTF8.GetBytes(message + "\n");
							await stream.WriteAsync(data, 0, data.Length, cancellationToken);
							await stream.FlushAsync(cancellationToken);

							_logger.LogInformation("Сообщение отправлено TCP-клиенту: {Message}", message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Ошибка при отправке сообщения TCP-клиенту");
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в процессе отправки сообщений TCP-клиенту");
			}
		}

		private async Task SendViaUdpAsync(
			UdpClient udpClient,
			IPEndPoint remoteEndPoint,
			string queueForListening,
			CancellationToken cancellationToken)
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
							await udpClient.SendAsync(data, data.Length, remoteEndPoint);

							_logger.LogInformation("Сообщение отправлено UDP-клиенту {RemoteEndPoint}: {Message}", remoteEndPoint, message);
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
