using servers_api.services.teaching;

namespace servers_api.middleware
{
	static class ProtocolProcessingConfiguration
	{
		public static IServiceCollection AddFactoryServices(this IServiceCollection services)
		{
			// Основные сервисы:
			services.AddTransient<ITeachIntegrationService, TeachIntegrationService>();

			return services;
		}
	}
}
