using servers_api.messaging.formatting;
using servers_api.messaging.processing;
using servers_api.messaging.sending;

namespace servers_api.middleware
{
	static class MessagingConfiguration
	{
		/// <summary>
		/// Регистрация сервисов, участвующих в отсылке и получении сообщений.
		/// </summary>
		public static IServiceCollection AddMessageServingServices(this IServiceCollection services)
		{
			services.AddTransient<IMessageSender, MessageSender>();
			services.AddTransient<IMessageFormatter, MessageFormatter>();
			services.AddTransient<IMessageProcessingService, MessageProcessingService>();

			return services;
		}
	}
}
