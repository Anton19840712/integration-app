namespace servers_api.middleware
{
	/// <summary>
	/// Класс обслуживает логику паттерна outbox.
	/// </summary>
	public static class OutboxConfiguration
	{
		public static IServiceCollection AddOutboxServices(this IServiceCollection services)
		{
			services.AddHostedService<OutboxMongoBackgroundService>();

			return services;
		}
	}
}
