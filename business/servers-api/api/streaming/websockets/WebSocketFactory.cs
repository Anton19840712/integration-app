using servers_api.api.streaming.websockets;
using servers_api.factory;

public class WebSocketFactory : UpInstanceByProtocolFactory
{
	private readonly ILogger<WebSocketFactory> _logger;
	private readonly WebSocketServerInstance _webSocketServer;
	private readonly WebSocketClientInstance _webSocketClient;

	public WebSocketFactory(
		ILogger<WebSocketFactory> logger,
		WebSocketServerInstance webSocketServer,
		WebSocketClientInstance webSocketClient
	)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_webSocketServer = webSocketServer ?? throw new ArgumentNullException(nameof(webSocketServer));
		_webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
	}

	public override IUpServer CreateServer()
	{
		_logger.LogInformation("Создание WebSocket-сервера.");
		return _webSocketServer;
	}

	public override IUpClient CreateClient()
	{
		_logger.LogInformation("Создание WebSocket-клиента.");
		return _webSocketClient;
	}
}
