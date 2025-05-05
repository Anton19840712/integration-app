using Microsoft.AspNetCore.Mvc;

namespace api.rest.functional.organizers
{
	[ApiController]
	[Route("api/test")]
	public class TestController : ControllerBase
	{
		private readonly ILogger<TestController> _logger;

		public TestController(ILogger<TestController> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Простой тестовый эндпоинт
		/// </summary>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
			Console.WriteLine($"Ping requested from {remoteIp}");
			_logger.LogInformation("Ping requested from {RemoteIp}", remoteIp);

			return Ok("Hello, world!");
		}
	}
}
