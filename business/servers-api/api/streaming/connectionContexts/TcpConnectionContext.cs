using System.Net.Sockets;

namespace servers_api.api.streaming.connectionContexts
{
	public class TcpConnectionContext : IConnectionContext
	{
		public TcpClient TcpClient { get; }

		public TcpConnectionContext(TcpClient tcpClient)
		{
			TcpClient = tcpClient;
		}

		public string Protocol => "tcp";
	}
}
