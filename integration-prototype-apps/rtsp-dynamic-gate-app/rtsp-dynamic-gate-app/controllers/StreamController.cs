using Microsoft.AspNetCore.Mvc;
using rtsp_dynamic_gate_app.background;

namespace rtsp_dynamic_gate_app.controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StreamController : ControllerBase
	{
		private readonly RtspStreamingService _streamingService;
		private readonly ILogger<StreamController> _logger;

		public StreamController(RtspStreamingService streamingService, ILogger<StreamController> logger)
		{
			_streamingService = streamingService;
			_logger = logger;
		}

		[HttpPost("start/{channelId}")]
		public IActionResult StartStream(int channelId, [FromQuery] int streamType = 0)
		{
			try
			{
				_streamingService.StartStream(channelId, streamType);
				return Ok(new { message = $"Stream started for channel {channelId}" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to start stream for channel {ChannelId}", channelId);
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpPost("stop/{channelId}")]
		public async Task<IActionResult> StopStream(int channelId)
		{
			try
			{
				await _streamingService.StopStreamAsync(channelId, CancellationToken.None);
				return Ok(new { message = $"Stream stopped for channel {channelId}" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to stop stream for channel {ChannelId}", channelId);
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("status/{channelId}")]
		public IActionResult StreamStatus(int channelId)
		{
			var isRunning = _streamingService.IsStreaming(channelId);
			return Ok(new
			{
				channelId,
				isRunning
			});
		}

		[HttpPost("stop-all")]
		public async Task<IActionResult> StopAll()
		{
			try
			{
				await _streamingService.StopAllStreamsAsync(CancellationToken.None);
				return Ok(new { message = "All streams stopped" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to stop all streams");
				return StatusCode(500, new { error = ex.Message });
			}
		}
	}
}