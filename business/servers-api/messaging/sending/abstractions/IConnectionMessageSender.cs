namespace servers_api.messaging.sending.abstractions
{
	public interface IConnectionMessageSender
	{
		Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken);
	}
}
