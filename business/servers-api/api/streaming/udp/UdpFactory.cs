using servers_api.factory;

namespace servers_api.api.streaming.udp
{
	public class UdpFactory : UpInstanceByProtocolFactory
	{
		private readonly ILogger<UdpFactory> _logger;
		private readonly UdpServerInstance _udpServer;
		private readonly UdpClientInstance _udpClient;

		public UdpFactory(
			ILogger<UdpFactory> logger,
			UdpServerInstance udpServer,
			UdpClientInstance udpClient
		)
		{
			_logger = logger;
			_udpServer = udpServer;
			_udpClient = udpClient;
		}

		public override IUpServer CreateServer()
		{
			_logger.LogInformation("Создание UDP-сервера.");
			return _udpServer;
		}

		public override IUpClient CreateClient()
		{
			_logger.LogInformation("Создание UDP-клиента.");
			return _udpClient;
		}
	}
}