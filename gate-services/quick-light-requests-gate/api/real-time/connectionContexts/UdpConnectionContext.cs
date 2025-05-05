using System.Net.Sockets;
using System.Net;

namespace servers_api.api.streaming.connectionContexts
{
	public class UdpConnectionContext : IConnectionContext
	{
		public UdpClient UdpClient { get; }
		public IPEndPoint RemoteEndPoint { get; }

		public UdpConnectionContext(UdpClient udpClient, IPEndPoint remoteEndPoint)
		{
			UdpClient = udpClient;
			RemoteEndPoint = remoteEndPoint;
		}

		public string Protocol => "udp";
	}
}
