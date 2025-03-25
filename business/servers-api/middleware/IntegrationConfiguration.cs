using Serilog;
using servers_api.Services.InternalSystems;
using servers_api.Services.Parsers;

namespace servers_api.middleware;

static class IntegrationConfiguration
{
	/// <summary>
	/// Регистрация API сервисов, участвующих в процессе интеграции.
	/// </summary>
	public static IServiceCollection AddApiServices(this IServiceCollection services)
	{
		Log.Information("Регистрация API-сервисов...");

		services.AddTransient<IJsonParsingService, JsonParsingService>();
		services.AddTransient<ITeachSenderHandler, TeachSenderHandler>();

		Log.Information("API-сервисы зарегистрированы.");

		return services;
	}
}
