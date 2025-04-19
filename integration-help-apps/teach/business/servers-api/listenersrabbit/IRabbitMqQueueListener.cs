namespace servers_api.listenersrabbit
{
	public interface IRabbitMqQueueListener<TListener> where TListener : class
	{
		Task<bool> StartListeningAsync(
			string queueOutName,
			CancellationToken stoppingToken,
			string pathToPushIn = null,
			Func<string, Task> onMessageReceived = null);
		void StopListening();
	}
}
