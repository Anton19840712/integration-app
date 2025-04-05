using FluentValidation;
using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.validation.common;

namespace servers_api.middleware
{
	static class ValidationConfiguration
	{
		/// <summary>
		/// Регистрация сервисов валидации.
		/// </summary>
		public static IServiceCollection AddValidationServices(this IServiceCollection services)
		{
			services.AddScoped<IServerInstanceFluentValidator, ServerInstanceFluentValidator>();
			services.AddScoped<IValidator<ServerInstanceModel>, ServerInstanceModelValidator>();

			return services;
		}
	}
}
