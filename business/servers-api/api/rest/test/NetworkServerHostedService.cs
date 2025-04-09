namespace servers_api.api.rest.test
{
	public class NetworkServerHostedService : BackgroundService
	{
		private readonly ILogger<NetworkServerHostedService> _logger;
		private readonly NetworkServerManager _manager;
		private readonly IConfiguration _configuration;

		public NetworkServerHostedService(
			ILogger<NetworkServerHostedService> logger,
			NetworkServerManager manager,
			IConfiguration configuration)
		{
			_logger = logger;
			_manager = manager;
			_configuration = configuration;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var protocol = _configuration["Protocol"]?.ToLowerInvariant();
			if (protocol == "tcp" || protocol == "udp")
			{
				_logger.LogInformation($"Автозапуск сервера: {protocol}");
				await _manager.StartServerAsync(protocol, stoppingToken);
			}
		}
	}
}
