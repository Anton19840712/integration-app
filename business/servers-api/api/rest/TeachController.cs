using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using servers_api.services.teaching;

namespace servers_api.api.rest
{
	[ApiController]
	[Route("api/teach")]
	public class TeachController : ControllerBase
	{
		private readonly ILogger<TeachController> _logger;
		private readonly ITeachIntegrationService _teachIntegrationService;

		public TeachController(
			ILogger<TeachController> logger,
			ITeachIntegrationService teachIntegrationService)
		{
			_logger = logger;
			_teachIntegrationService = teachIntegrationService;
		}

		/// <summary>
		/// Обучает интеграцию на основе переданных данных
		/// </summary>
		[HttpPost("teach")]
		public async Task<IActionResult> Teach([FromBody] JsonElement jsonBody, CancellationToken cancellationToken)
		{
			try
			{
				_logger.LogInformation("Upload endpoint called with body: {JsonBody}", jsonBody.ToString());

				var result = await _teachIntegrationService.TeachAsync(jsonBody, cancellationToken);

				_logger.LogInformation("File uploaded successfully");
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during file upload");
				return Problem(ex.Message);
			}
		}
	}
}
