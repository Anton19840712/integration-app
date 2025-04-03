using servers_api.api.streaming.tcp;
using servers_api.api.streaming.udp;
using servers_api.api.streaming.websockets;
using servers_api.factory;
using servers_api.main.services;

namespace servers_api.middleware
{
	static class ProtocolProcessingConfiguration
	{
		public static IServiceCollection AddFactoryServices(this IServiceCollection services)
		{
			// Менеджер протоколов
			services.AddTransient<IProtocolManager, ProtocolManager>();

			// Добавляем зависимые классы, которые используются в фабриках
			services.AddTransient<TcpClientInstance>();
			services.AddTransient<TcpServerInstance>();

			services.AddTransient<UdpClientInstance>();
			services.AddTransient<UdpServerInstance>();

			services.AddTransient<WebSocketClientInstance>();
			services.AddTransient<WebSocketServerInstance>();


			// Регистрируем фабрики с областью жизни Scoped
			services.AddScoped<TcpFactory>();
			services.AddScoped<UdpFactory>();
			services.AddScoped<WebSocketFactory>();

			// Регистрируем словарь с фабриками как Singleton
			services.AddSingleton(provider =>
			{
				return new Dictionary<string, UpInstanceByProtocolFactory>
				{
					{ "tcp", provider.GetRequiredService<TcpFactory>() },
					{ "udp", provider.GetRequiredService<UdpFactory>() },
					{ "ws", provider.GetRequiredService<WebSocketFactory>() }
				};
			});

			// Основные сервисы:
			services.AddTransient<ITeachIntegrationService, TeachIntegrationService>();

			return services;
		}
	}
}
