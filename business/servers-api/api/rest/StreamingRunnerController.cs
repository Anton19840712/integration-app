using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using servers_api.factory;
using servers_api.Services.Parsers;

namespace servers_api.api.rest
{
	[ApiController]
	[Route("api/streaming")]
	public class StreamingRunnerController : ControllerBase
	{
		private readonly ILogger<StreamingRunnerController> _logger;
		private readonly IJsonParsingService _jsonParsingService;
		private readonly IProtocolManager _protocolManager;

		public StreamingRunnerController(
			ILogger<StreamingRunnerController> logger,
			IJsonParsingService jsonParsingService,
			IProtocolManager protocolManager)
		{
			_logger = logger;
			_jsonParsingService = jsonParsingService;
			_protocolManager = protocolManager;
		}

		/// <summary>
		/// Запускает серверный протокол на основе конфигурации
		/// </summary>
		[HttpPost("run")]
		public async Task<IActionResult> Run([FromBody] JsonElement jsonBody)
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			var cancellationToken = cts.Token;

			try
			{
				_logger.LogInformation("Start server endpoint called with body: {JsonBody}", jsonBody.ToString());

				var parsedModel = await _jsonParsingService.ParseJsonAsync(
					jsonBody,
					isTeaching: false,
					cancellationToken);

				var apiStatus = await _protocolManager.UpNodeAsync(parsedModel, cancellationToken);

				_logger.LogInformation("Node configured successfully");

				return Ok(apiStatus);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("Operation was canceled due to timeout");
				return Problem("Operation timed out");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during server run");
				return Problem(ex.Message);
			}
		}
	}
}
