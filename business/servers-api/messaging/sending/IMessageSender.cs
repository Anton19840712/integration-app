using System.Net.Sockets;

namespace servers_api.messaging.sending
{
	/// <summary>
	/// Отсылает сообщение на внешний клиент.
	/// </summary>
	public interface IMessageSender
	{
		Task SendMessagesToClientAsync(
			TcpClient client,
			string queueForListening,
			CancellationToken cancellationToken);
	}
}
