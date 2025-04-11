using System.Net.WebSockets;

namespace servers_api.api.streaming.connectionContexts
{
	public class WebSocketConnectionContext : IConnectionContext
	{
		public WebSocket Socket { get; }

		public string Protocol => "websocket";

		public WebSocketConnectionContext(WebSocket socket) => Socket = socket;
	}
}


