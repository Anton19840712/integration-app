namespace servers_api.middleware
{
	/// <summary>
	/// Класс для регистрации различных http сервисов.
	/// </summary>
	static class HttpConfiguration
	{
		public static IServiceCollection AddHttpServices(this IServiceCollection services)
		{
			services.AddHttpClient();
			services.AddHttpContextAccessor();

			return services;
		}
	}
}
