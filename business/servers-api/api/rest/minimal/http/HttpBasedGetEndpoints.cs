using System.Text;

namespace servers_api.api.rest.minimal.http
{
	// // we are a server here, postman is a client and receives get requests

	public static class HttpBasedGetEndpoints
	{
		/// <summary>
		/// Этот endpoint будет отсылать данные на postman как sse-server на клиент. Клиентом в этом случае будет выступать postman.
		/// </summary>
		/// <param name="app"></param>
		/// <param name="loggerFactory"></param>
		public static void MapHttpBasedGetEndpoints(
			this IEndpointRouteBuilder app,
			ILoggerFactory loggerFactory)
		{
			var logger = loggerFactory.CreateLogger("HttpBasedEndpoints");

			app.MapGet("/sse", async (context) =>
			{
				context.Response.Headers.Append("Content-Type", "text/event-stream");
				context.Response.Headers.Append("Cache-Control", "no-cache");
				context.Response.Headers.Append("Connection", "keep-alive");
				context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

				var messageCounter = 0;
				var stream = context.Response.BodyWriter.AsStream();
				var cancellationToken = context.RequestAborted;

				logger.LogInformation("New SSE connection established.");

				try
				{
					while (!cancellationToken.IsCancellationRequested)
					{
						messageCounter++;
						var data = $"data: Message #{messageCounter} - Server time is {DateTime.Now}\n\n";
						var buffer = Encoding.UTF8.GetBytes(data);

						await stream.WriteAsync(buffer, cancellationToken);
						await stream.FlushAsync(cancellationToken);

						logger.LogInformation($"Sent data: {data.Trim()}");
						await Task.Delay(1000, cancellationToken); // Отправляем сообщение каждую секунду
					}
				}
				catch (OperationCanceledException)
				{
					logger.LogInformation("SSE connection closed.");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error in SSE stream.");
				}
			});

			app.MapGet("/long-polling", async (HttpContext context) =>
			{
				var timeout = 30_000; // 30 секунд
				var startTime = DateTime.UtcNow;

				while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
				{
					// Здесь должна быть логика проверки новых данных (например, из БД или очереди)
					var hasNewData = DateTime.UtcNow.Second % 10 == 0; // Симуляция новых данных раз в 10 секунд

					if (hasNewData)
					{
						var newMessage = $"New Message at {DateTime.Now}";
						logger.LogInformation($"Long Polling: {newMessage}");
						return Results.Ok(new { message = newMessage });
					}

					// Ждём перед следующей проверкой, чтобы не грузить процессор
					await Task.Delay(500, context.RequestAborted);
				}

				// Если данных не было в течение таймаута, вернём пустое сообщение
				return Results.Ok(new { message = "No new messages" });
			});


			app.MapGet("/short-polling", () =>
			{
				//клиент каждый раз повторяет определенный запрос (можно настроить интервальный запрос через postman на количество раз, чтобы вызызывать данное api)
				//симулируя поведение запросов со стороны потенциального клиента.
				var message = $"Message - Server time is {DateTime.Now}";
				logger.LogInformation($"Short Polling: {message}");
				return Results.Ok(new { message });
			});
		}
	}
}
