using System.Text;

namespace servers_api.api.rest.minimal.http
{
	public static class HttpBasedPostEndpoints
	{
		/// <summary>
		/// Этот endpoint получает сообщения из postman.
		/// </summary>
		/// <param name="app"></param>
		/// <param name="loggerFactory"></param>
		public static void MapHttpBasedPostEndpoints(
			this IEndpointRouteBuilder app,
			ILoggerFactory loggerFactory)
		{
			var logger = loggerFactory.CreateLogger("HttpBasedEndpoints");

			// 1. SSE-like с использованием POST (постман шлёт запросы - сервер отвечает сразу)
			app.MapPost("/post-sse", async (context) =>
			{
				var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
				logger.LogInformation($"Received POST for SSE: {body}");

				var responseMessage = $"SSE-like Response: Received at {DateTime.Now}";
				await context.Response.WriteAsync(responseMessage);
			});

			// 2. Long Polling с POST (постман ждёт, пока сервер не отправит данные)
			app.MapPost("/post-long-polling", async (HttpContext context) =>
			{
				var timeout = 30_000; // 30 секунд:
				var startTime = DateTime.UtcNow;
				logger.LogInformation("Post Long Polling: Waiting for new data...");

				// пока 30 секунд не прошло (за это время мы ожидаем :
				while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
				{
					var hasNewData = DateTime.UtcNow.Second % 10 == 0;
					if (hasNewData)
					{
						var newMessage = $"Long Polling Message: {DateTime.Now}";
						logger.LogInformation($"Post Long Polling: {newMessage}");
						return Results.Ok(new { message = newMessage });
					}
					await Task.Delay(500, context.RequestAborted);
				}

				return Results.Ok(new { message = "No new messages" });
			});

			// 3. Short Polling с POST (постман выступает как сервер и шлет сюда свои сообщения, мы выступаем здесь как клиент)
			app.MapPost("/post-short-polling", async (HttpContext context) =>
			{
				var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
				logger.LogInformation($"Short Polling Received: {body}");

				var responseMessage = $"Short Polling Response: {DateTime.Now}";
				return Results.Ok(new { message = responseMessage });
			});
		}
	}
}