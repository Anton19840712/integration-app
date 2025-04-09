﻿using Microsoft.AspNetCore.Mvc;

namespace servers_api.api.rest.test
{
	[ApiController]
	[Route("api/[controller]")]
	public class ServerControlController : ControllerBase
	{
		private readonly NetworkServerManager _manager;

		public ServerControlController(NetworkServerManager manager)
		{
			_manager = manager;
		}

		[HttpPost("start/{protocol}")]
		public async Task<IActionResult> Start(string protocol)
		{
			try
			{
				await _manager.StartServerAsync(protocol, HttpContext.RequestAborted);
				return Ok($"Сервер {protocol} запущен.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost("stop/{protocol}")]
		public async Task<IActionResult> Stop(string protocol)
		{
			try
			{
				await _manager.StopServerAsync(protocol, HttpContext.RequestAborted);
				return Ok($"Сервер {protocol} остановлен.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("status")]
		public IActionResult Status()
		{
			var running = _manager.GetRunningServers();
			return Ok(running);
		}
	}
}
