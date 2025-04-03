namespace servers_api.middleware
{
	static class CommonConfiguration
	{
		/// <summary>
		/// Регистрация сервисов общего назначения.
		/// </summary>
		public static IServiceCollection AddCommonServices(this IServiceCollection services)
		{
			services.AddCors();

			return services;
		}
	}
}
