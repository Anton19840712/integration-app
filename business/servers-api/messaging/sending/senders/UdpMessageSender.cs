﻿using servers_api.listenersrabbit;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace servers_api.messaging.sending.senders
{
	public class UdpMessageSender : BaseMessageSender<UdpMessageSender>
	{
		private readonly UdpClient _udpClient;
		private readonly IPEndPoint _remoteEndPoint;

		public UdpMessageSender(
						UdpClient udpClient,
						IPEndPoint remoteEndPoint,
						IRabbitMqQueueListener<RabbitMqQueueListener> rabbitMqQueueListener,
						ILogger<UdpMessageSender> logger) : base(rabbitMqQueueListener, logger)
		{
			_udpClient = udpClient;
			_remoteEndPoint = remoteEndPoint;
		}

		protected override async Task SendToClientAsync(string message, CancellationToken cancellationToken)
		{
			byte[] data = Encoding.UTF8.GetBytes(message);
			await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);
		}
	}
}
