using Serilog;

namespace servers_api.middleware;

/// <summary>
/// Класс обслуживает логику паттерна outbox.
/// </summary>
public static class OutboxConfiguration
{
	public static IServiceCollection AddOutboxServices(this IServiceCollection services)
	{
		Log.Information("Регистрация OutboxProcessor...");

		// Регистрируем как IHostedService для фонового выполнения

		services.AddHostedService<OutboxMongoBackgroundService>();

		Log.Information("OutboxProcessor зарегистрирован.");
		return services;
	}
}
