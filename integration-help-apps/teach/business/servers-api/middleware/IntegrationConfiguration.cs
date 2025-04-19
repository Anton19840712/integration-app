using servers_api.services.teaching;

namespace servers_api.middleware
{
	static class IntegrationConfiguration
	{
		/// <summary>
		/// Регистрация API сервисов, участвующих в процессе интеграции.
		/// </summary>
		public static IServiceCollection AddApiServices(this IServiceCollection services)
		{
			services.AddTransient<IJsonParsingService, JsonParsingService>();
			services.AddTransient<ITeachSenderHandler, TeachSenderHandler>();

			return services;
		}
	}
}
