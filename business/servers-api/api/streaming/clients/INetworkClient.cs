namespace servers_api.api.streaming.clients
{
	public interface INetworkClient
	{
		string Protocol { get; }
		bool IsRunning { get; }
		Task StartAsync(CancellationToken cancellationToken);
		Task StopAsync(CancellationToken cancellationToken);
	}
}
