using servers_api.services.senders;

namespace servers_api.middleware
{
	static class HostedServicesConfiguration
	{
		/// <summary>
		/// Регистрация фоновых сервисов приложения.
		/// </summary>
		public static IServiceCollection AddHostedServices(this IServiceCollection services)
		{
			return services;
		}
	}
}
