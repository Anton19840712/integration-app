using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using servers_api.factory;
using servers_api.main.services;
using servers_api.Services.Parsers;

namespace servers_api.api.minimal;

public static class IntegrationEndpoints
{
	public static void MapIntegrationMinimalApi(
		this IEndpointRouteBuilder app,
		ILoggerFactory loggerFactory)
	{
		var logger = loggerFactory.CreateLogger("IntegrationEndpoints");

		app.MapPost("/api/servers/teach", async (
			[FromBody] JsonElement jsonBody,
			ITeachIntegrationService uploadFileService,
			CancellationToken stoppingToken) =>
		{
			try
			{
				logger.LogInformation("Upload endpoint called with body: {JsonBody}", jsonBody.ToString());
				var result = await uploadFileService.TeachAsync(jsonBody, stoppingToken);
				logger.LogInformation("File uploaded successfully");
				return Results.Ok(result);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during file upload");
				return Results.Problem(ex.Message);
			}
		});

		app.MapPost("/api/servers/run", async (
			[FromBody] JsonElement jsonBody,
			IJsonParsingService jsonParsingService,
			IProtocolManager protocolManager,
			IServiceProvider serviceProvider) =>
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			var stoppingToken = cts.Token;

			try
			{
				logger.LogInformation("Start server endpoint called with body: {JsonBody}", jsonBody.ToString());

				var parsedModel = await jsonParsingService.ParseJsonAsync(
					jsonBody,
					isTeaching: false,
					stoppingToken);

				var apiStatus = await protocolManager.UpNodeAsync(parsedModel, stoppingToken);

				logger.LogInformation("Node configured successfully");

				return Results.Ok(apiStatus);
			}
			catch (OperationCanceledException)
			{
				logger.LogWarning("Operation was canceled due to timeout");
				return Results.Problem("Operation timed out");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during server run");
				return Results.Problem(ex.Message);
			}
		});
	}
}