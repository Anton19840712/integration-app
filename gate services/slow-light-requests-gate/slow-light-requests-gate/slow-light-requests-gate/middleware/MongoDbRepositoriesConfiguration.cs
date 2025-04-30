using lazy_light_requests_gate.entities;
using lazy_light_requests_gate.repositories;

namespace lazy_light_requests_gate.middleware
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
