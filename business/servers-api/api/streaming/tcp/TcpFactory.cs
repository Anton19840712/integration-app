using servers_api.api.streaming.tcp;
using servers_api.factory;

namespace servers_api.api.streaming.tcp
{
	public class TcpFactory : UpInstanceByProtocolFactory
	{
		private readonly ILogger<TcpFactory> _logger;
		private readonly TcpServerInstance _tcpServer;
		private readonly TcpClientInstance _tcpClient;

		public TcpFactory(
			ILogger<TcpFactory> logger,
			TcpServerInstance tcpServer,
			TcpClientInstance tcpClient
		)
		{
			_logger = logger;
			_tcpServer = tcpServer;
			_tcpClient = tcpClient;
		}

		public override IUpServer CreateServer()
		{
			_logger.LogInformation("Создание TCP-сервера.");
			return _tcpServer;
		}

		public override IUpClient CreateClient()
		{
			_logger.LogInformation("Создание TCP-клиента.");
			return _tcpClient;
		}
	}
}