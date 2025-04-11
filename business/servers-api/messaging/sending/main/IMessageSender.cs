using servers_api.api.rest.test.connectionContexts;

namespace servers_api.messaging.sending.main
{
	/// <summary>
	/// Отсылает сообщение на внешний клиент.
	/// </summary>
	public interface IMessageSender
	{
		Task SendMessagesToClientAsync(
			IConnectionContext connectionContext,
			string queueForListening,
			CancellationToken cancellationToken);
	}
}
