using servers_api.factory;
using servers_api.protocols.http;

public class HttpFactory : UpInstanceByProtocolFactory
{
	private readonly ILogger<HttpFactory> _logger;
	private readonly HttpServerInstance _httpServer;
	private readonly HttpClientInstance _httpClient;

	public HttpFactory(
		ILogger<HttpFactory> logger,
		HttpServerInstance httpServer,
		HttpClientInstance httpClient
	)
	{
		_logger = logger;
		_httpServer = httpServer;
		_httpClient = httpClient;
	}

	public override IUpServer CreateServer()
	{
		_logger.LogInformation("Создание HTTP-сервера.");
		return _httpServer;
	}

	public override IUpClient CreateClient()
	{
		_logger.LogInformation("Создание HTTP-клиента.");
		return _httpClient;
	}
}
