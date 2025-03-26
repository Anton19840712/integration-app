namespace servers_api.api.rest.minimal.common
{
	public static class TestCheckEndpoints
	{
		public static void MapTestMinimalApi(
			this IEndpointRouteBuilder app,
			ILoggerFactory loggerFactory)
		{
			var logger = loggerFactory.CreateLogger("TestEndpoints");

			// Тестовый эндпоинт /api/ping
			app.MapGet("/api/ping", (HttpContext context) =>
			{
				Console.WriteLine($"Ping requested from {context.Connection.RemoteIpAddress}");
				return Results.Ok("Hello, world!");
			});
		}
	}
}