using servers_api.validation.headers;

namespace servers_api.middleware
{
	static class HeadersConfiguration
	{
		/// <summary>
		/// Регистрация сервисов заголовков.
		/// </summary>
		public static IServiceCollection AddHeadersServices(this IServiceCollection services)
		{
			services.AddTransient<SimpleHeadersValidator>();
			services.AddTransient<DetailedHeadersValidator>();
			services.AddTransient<IHeaderValidationService, HeaderValidationService>();

			return services;
		}
	}
}
