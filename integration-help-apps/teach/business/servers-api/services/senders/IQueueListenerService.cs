namespace servers_api.services.senders
{
	public interface IQueueListenerService
	{
		Task ExecuteAsync(CancellationToken stoppingToken);
	}
}
