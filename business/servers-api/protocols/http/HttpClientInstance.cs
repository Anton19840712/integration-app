using System.Text;
using servers_api.factory;
using servers_api.models.internallayer.instance;
using servers_api.models.response;

public class HttpClientInstance : IUpClient
{
	private readonly ILogger<HttpClientInstance> _logger;
	private static readonly HttpClient _httpClient = new(); // Глобальный HttpClient
	private readonly string _serverUrl = "http://localhost:52799/sse/"; // Адрес сервера SSE

	public HttpClientInstance(ILogger<HttpClientInstance> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<ResponseIntegration> ConnectToServerAsync(ClientInstanceModel instanceModel,
		string serverHost,
		int serverPort,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("SSE Client is starting...");
		int reconnectDelay = 5000; // 5 секунд перед повторной попыткой

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, _serverUrl);
				request.Headers.Add("Accept", "text/event-stream");

				using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				response.EnsureSuccessStatusCode();

				_logger.LogInformation("Connected to SSE server.");

				await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
				using var reader = new StreamReader(stream, Encoding.UTF8);

				await ProcessSSEStreamAsync(reader, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				_logger.LogInformation("SSE client task was cancelled.");
				break;
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Network error while connecting to SSE server. Retrying in {Delay} ms...", reconnectDelay);
			}
			catch (IOException ex)
			{
				_logger.LogWarning(ex, "Connection was closed unexpectedly. Retrying in {Delay} ms...");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in SSE client. Retrying in {Delay} ms...");
			}

			await Task.Delay(reconnectDelay, cancellationToken);
		}

		_logger.LogWarning("SSE Client stopped.");
		return default;
	}

	private async Task ProcessSSEStreamAsync(StreamReader reader, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var line = await reader.ReadLineAsync();
				if (line == null)
				{
					_logger.LogWarning("SSE stream ended unexpectedly.");
					break;
				}

				if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
				{
					var content = line[6..].Trim();
					_logger.LogInformation("Received SSE message: {Message}", content);
				}
			}
			catch (IOException ex)
			{
				_logger.LogWarning(ex, "Stream was interrupted.");
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while processing SSE message.");
			}
		}

		_logger.LogWarning("SSE stream ended. Reconnecting...");
	}
}
