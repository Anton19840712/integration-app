namespace sftp_dynamic_gate_app.listeners
{
	public interface IRabbitMqQueueListener<TListener> where TListener : class
	{
		Task StartListeningAsync(
			string queueOutName,
			CancellationToken stoppingToken,
			string pathToPushIn = null,
			Func<string, Task> onMessageReceived = null);
		void StopListening();
	}
}
