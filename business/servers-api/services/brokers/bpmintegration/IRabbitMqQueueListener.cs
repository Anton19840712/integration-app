namespace servers_api.services.brokers.bpmintegration
{
	public interface IRabbitMqQueueListener<TListener> where TListener : class
	{
		Task StartListeningAsync(
			string queueOutName,
			CancellationToken stoppingToken,
			string pathForSave = null);
		void StopListening();
	}
}
