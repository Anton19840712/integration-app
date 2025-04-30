namespace messaging.sending.abstractions
{
	public interface IConnectionMessageSender
	{
		Task SendMessageAsync(string queueForListening, CancellationToken cancellationToken);
	}
}
