﻿using Microsoft.AspNetCore.Mvc;

namespace servers_api.api.rest.test
{
	[ApiController]
	[Route("api/[controller]")]
	public class ClientStreamControlController : ControllerBase
	{
		private readonly NetworkClientManager _manager;

		public ClientStreamControlController(NetworkClientManager manager)
		{
			_manager = manager;
		}

		[HttpPost("start/{protocol}")]
		public async Task<IActionResult> Start(string protocol)
		{
			try
			{
				await _manager.StartClientAsync(protocol, HttpContext.RequestAborted);
				return Ok($"Клиент {protocol} запущен.");
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
				await _manager.StopClientAsync(protocol, HttpContext.RequestAborted);
				return Ok($"Клиент {protocol} остановлен.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("status")]
		public IActionResult Status()
		{
			var running = _manager.GetRunningClients();
			return Ok(running);
		}
	}
}
