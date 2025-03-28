using System.Text;

namespace servers_api.api.rest.minimal.http
{
	// we are a client here, postman is a server and sents post requests

	public static class HttpBasedPostEndpoints
	{
		/// <summary>
		/// Этот endpoint получает сообщения из Postman.
		/// </summary>
		/// <param name="app"></param>
		/// <param name="loggerFactory"></param>
		public static void MapHttpBasedPostEndpoints(
			this IEndpointRouteBuilder app,
			ILoggerFactory loggerFactory)
		{
			var logger = loggerFactory.CreateLogger("HttpBasedEndpoints");

			// Вспомогательный метод для логирования заголовков
			void LogHeaders(HttpContext context)
			{
				logger.LogInformation("Headers Received:");
				foreach (var header in context.Request.Headers)
				{
					logger.LogInformation($"  {header.Key}: {header.Value}");
				}
			}

			// 1. SSE-like с использованием POST (Postman шлёт запросы - сервер отвечает сразу)
			app.MapPost("/post-sse", async (context) =>
			{
				LogHeaders(context);

				// валидация
				// я как пнр выбираю какой именно сервис валидации (могу именно настроить путь до этого эндпоинта) отвечает за именно этот инстанс шлюза
				// условно 


				// приземление
				// пульнули в шину
				// ответили обратно на основании ответа от сервиса валидации заголовков
				// то как оно пришло, оно должно шпульнуться в шину

				var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
				logger.LogInformation($"Received POST for SSE: {body}");

				var responseMessage = $"SSE-like Response: Received at {DateTime.Now}";
				await context.Response.WriteAsync(responseMessage);
			});

			// 2. Long Polling с POST (Postman ждёт, пока сервер не отправит данные)
			app.MapPost("/post-long-polling", async (HttpContext context) =>
			{
				LogHeaders(context);

				var timeout = 30_000; // 30 секунд
				var startTime = DateTime.UtcNow;
				logger.LogInformation("Post Long Polling: Waiting for new data...");

				// Пока 30 секунд не прошло (за это время мы ожидаем новые данные)
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
				// приземлили
				// отдали в bpme - дождаться обратную связь от bpme (что-то должно прийти и я как-то должен отреагировать)
				// ответили обратно
				return Results.Ok(new { message = "No new messages" });
			});

			// 3. Short Polling с POST (Postman выступает как сервер и шлёт сюда сообщения, мы клиент)
			app.MapPost("/post-short-polling", async (HttpContext context) =>
			{
				LogHeaders(context);

				var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
				logger.LogInformation($"Short Polling Received: {body}");

				// приземление
				// пульнули в шину
				// ответили обратно
				// то как оно пришло, оно должно шпульнуться в шину

				var responseMessage = $"Short Polling Response: {DateTime.Now}";
				return Results.Ok(new { message = responseMessage });
			});
		}
	}
}