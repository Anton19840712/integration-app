namespace servers_api.messaging.sending
{
	public interface IConnectionMessageSender
	{
		Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken);
	}
}
