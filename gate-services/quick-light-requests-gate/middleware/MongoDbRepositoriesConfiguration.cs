using models.dynamicgatesettings.entities;
using models.outbox;
using repositories;

namespace middleware
{
	static class MongoDbRepositoriesConfiguration
	{
		/// <summary>
		/// Регистрация MongoDB клиента и связная с работой с данной базой логика.
		/// </summary>
		public static IServiceCollection AddMongoDbRepositoriesServices(this IServiceCollection services, IConfiguration configuration)
		{

			services.AddTransient<IMongoRepository<QueuesEntity>, MongoRepository<QueuesEntity>>();
			services.AddTransient<IMongoRepository<OutboxMessage>, MongoRepository<OutboxMessage>>();
			services.AddTransient<IMongoRepository<IncidentEntity>, MongoRepository<IncidentEntity>>();

			services.AddSingleton<MongoRepository<OutboxMessage>>();
			services.AddSingleton<MongoRepository<QueuesEntity>>();
			services.AddSingleton<MongoRepository<IncidentEntity>>();

			return services;
		}
	}
}
