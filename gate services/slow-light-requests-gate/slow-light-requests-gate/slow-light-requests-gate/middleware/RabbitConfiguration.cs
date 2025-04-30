using lazy_light_requests_gate.configurationsettings;
using lazy_light_requests_gate.listenersrabbit;
using lazy_light_requests_gate.rabbitqueuesconnections;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog;

namespace lazy_light_requests_gate.middleware
{
	public static class RabbitConfiguration
	{
		/// <summary>
		/// Регистрация RabbitMQ сервисов.
		/// </summary>
		public static IServiceCollection AddRabbitMqServices(this IServiceCollection services, IConfiguration configuration)
		{

			// Регистрация настроек RabbitMq в DI-контейнере
			services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMqSettings"));

			// Добавление IConnectionFactory с использованием настроек RabbitMq
			services.AddSingleton<IConnectionFactory>(provider =>
			{
				var rabbitMqSettings = provider.GetRequiredService<IOptions<RabbitMqSettings>>()?.Value;

				if (rabbitMqSettings == null)
				{
					throw new InvalidOperationException("Конфигурация RabbitMQ отсутствует!");
				}

				if (string.IsNullOrWhiteSpace(rabbitMqSettings.HostName) ||
					rabbitMqSettings.Port == 0 ||
					string.IsNullOrWhiteSpace(rabbitMqSettings.UserName) ||
					string.IsNullOrWhiteSpace(rabbitMqSettings.Password))
				{
					throw new InvalidOperationException("Некорректные настройки RabbitMQ! Проверьте конфигурацию.");
				}

				var factory = new ConnectionFactory
				{
					HostName = rabbitMqSettings.HostName,
					Port = rabbitMqSettings.Port,
					UserName = rabbitMqSettings.UserName,
					Password = rabbitMqSettings.Password
				};

				Log.Information("RabbitMQ настроен: {Host}:{Port}", factory.HostName, factory.Port);
				return factory;
			});

			services.AddSingleton<IRabbitMqService, RabbitMqService>();
			services.AddSingleton<IRabbitMqQueueListener<RabbitMqQueueListener>, RabbitMqQueueListener>();

			services.AddSingleton<IConnectionFactory, ConnectionFactory>(_ => new ConnectionFactory { HostName = "localhost" });
			services.AddScoped<IRabbitMqQueueListener<RabbitMqQueueListener>, RabbitMqQueueListener>();

			return services;
		}
	}
}
