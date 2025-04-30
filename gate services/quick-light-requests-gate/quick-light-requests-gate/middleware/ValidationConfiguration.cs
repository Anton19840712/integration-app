using FluentValidation;
using models.dynamicgatesettings.internalusage;
using validation.common;

namespace middleware
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
