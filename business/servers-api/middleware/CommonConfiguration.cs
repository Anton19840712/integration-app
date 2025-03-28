﻿using Serilog;

namespace servers_api.middleware
{
	static class CommonConfiguration
	{
		/// <summary>
		/// Регистрация сервисов общего назначения.
		/// </summary>
		public static IServiceCollection AddCommonServices(this IServiceCollection services)
		{
			Log.Information("Регистрация базовых сервисов...");

			services.AddCors();

			Log.Information("Базовые сервисы зарегистрированы.");

			return services;
		}
	}
}
