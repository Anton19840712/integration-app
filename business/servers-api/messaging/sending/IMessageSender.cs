using System.Net.Sockets;

namespace servers_api.messaging.sending
{
	/// <summary>
	/// Отсылает сообщение на внешний клиент.
	/// </summary>
	public interface IMessageSender
	{
		Task SendMessagesToClientAsync(
			TcpClient tcpClient,
			string queueForListening,
			CancellationToken cancellationToken);
	}
}
