using messaging.formatting;
using messaging.processing;
using messaging.sending.main;

namespace middleware
{
	static class MessagingConfiguration
	{
		/// <summary>
		/// Регистрация сервисов, участвующих в отсылке и получении сообщений.
		/// </summary>
		public static IServiceCollection AddMessageServingServices(this IServiceCollection services)
		{
			services.AddScoped<IMessageSender, MessageSender>();
			services.AddTransient<IMessageFormatter, MessageFormatter>();
			services.AddTransient<IMessageProcessingService, MessageProcessingService>();

			return services;
		}
	}
}
