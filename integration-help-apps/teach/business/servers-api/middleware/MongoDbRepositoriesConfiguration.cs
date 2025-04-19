using servers_api.models.dynamicgatesettings.entities;
using servers_api.repositories;

namespace servers_api.middleware
{
	static class MongoDbRepositoriesConfiguration
	{
		/// <summary>
		/// Регистрация MongoDB клиента и связная с работой с данной базой логика.
		/// </summary>
		public static IServiceCollection AddMongoDbRepositoriesServices(this IServiceCollection services, IConfiguration configuration)
		{

			services.AddTransient<IMongoRepository<QueuesEntity>, MongoRepository<QueuesEntity>>();
			services.AddSingleton<MongoRepository<QueuesEntity>>();

			return services;
		}
	}
}
