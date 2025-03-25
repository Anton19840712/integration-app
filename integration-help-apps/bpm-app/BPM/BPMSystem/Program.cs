using BPMEngine.DB.Consts;
using BPMIntegration.Services.Background.BPMIntegration.Services.Background;
using BPMMessaging.mapping;
using BPMMessaging.models.dtos;
using BPMMessaging.models.entities;
using BPMMessaging.models.settings;
using BPMMessaging.parsing;
using BPMMessaging.publishing;
using BPMMessaging.repository;
using BPMSystem.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using RabbitMQ.Client;
using Serilog;

namespace BPMSystem
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			Console.Title = "server bpm";
			var builder = WebApplication.CreateBuilder(args);

			builder.Host.UseSerilog((ctx, cfg) => cfg
					   .ReadFrom.Configuration(ctx.Configuration)
					   .WriteTo.Console());

			// Извлечение строк подключения
			DBConsts.DBConnections.ConnectionString_BPM = builder.Configuration.GetConnectionString("BPM_db");
			DBConsts.DBConnections.ConnectionString_BLL = builder.Configuration.GetConnectionString("BLL_db");

			// Добавление сервисов
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "BPM", Version = "V1" });
			});

			// Настройка DbContext для работы с PostgreSQL через Npgsql
			builder.Services.AddDbContextPool<BPMContext>(opt =>
			{
				if (!string.IsNullOrEmpty(DBConsts.DBConnections.ConnectionString_BPM))
				{
					opt.UseNpgsql(DBConsts.DBConnections.ConnectionString_BPM,
						sqlopt =>
						{
							sqlopt.EnableRetryOnFailure();
							sqlopt.CommandTimeout(60);
						});
					opt.EnableSensitiveDataLogging();
					opt.EnableDetailedErrors();
				}
			});

			// Настройка RabbitMQ и MongoDB через IOptions
			builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
			builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

			// Добавление остальных сервисов
			builder.Services.AddSingleton<IMessagePublisher, MessagePublisher>();
			builder.Services.AddScoped<IJsonParsingService, JsonParsingService>();

			builder.Services.AddHostedService<OutboxIntegrationTrackingService>();


			builder.Services.AddSingleton<IMongoClient>(sp =>
			{
				var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
				return new MongoClient(settings.ConnectionString);
			});

			builder.Services.AddSingleton<IMongoDatabase>(sp =>
			{
				var mongoClient = sp.GetRequiredService<IMongoClient>();
				var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];
				return mongoClient.GetDatabase(databaseName);
			});



			builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
			builder.Services.AddSingleton<IMongoRepository<IncidentEntity>, MongoRepository<IncidentEntity>>();
			builder.Services.AddSingleton<IMongoRepository<OutboxMessage>, MongoRepository<OutboxMessage>>();


			// Создание фабрики RabbitMQ с использованием IOptions
			builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>(sp =>
			{
				var rabbitSettings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
				return new ConnectionFactory
				{
					HostName = rabbitSettings.HostName,
					Port = rabbitSettings.Port,
					UserName = rabbitSettings.UserName,
					Password = rabbitSettings.Password
				};
			});

			builder.Services.AddAutoMapper(typeof(MappingProfile));
			builder.Services.AddTransient<RabbitMqQueueListener>();
			builder.Services.AddSingleton<QueueListenerService>();

			var app = builder.Build();

			// Логируем запуск приложения
			Log.Information("Приложение запущено");

			// Настройка middleware для Swagger и других функций
			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseSwagger();
			app.UseSwaggerUI();
			app.UseHttpsRedirection();
			app.UseCors(x => x.SetIsOriginAllowed(origin => true)
						.AllowAnyMethod()
						.AllowAnyHeader()
						.AllowCredentials());
			app.UseRouting();
			app.MapControllers();

			var cts = new CancellationTokenSource();

			var queueListenerService = app.Services.GetRequiredService<QueueListenerService>();

			try
			{
				var consumers = await queueListenerService.StartQueueListenersAsync(cts.Token);

				if (consumers.Any())
				{
					Log.Information("Успешно запущены {Count} обработчиков очередей.", consumers.Count);

					// Регистрируем обработчик завершения приложения для корректного завершения работы слушателей
					app.Lifetime.ApplicationStopping.Register(() =>
					{
						Log.Information("Остановка приложения: завершаем работу слушателей очередей...");
						try
						{
							// Отправляем сигнал завершения потокам
							cts.Cancel();

							// Даём немного времени слушателям завершиться
							Task.Delay(2000).Wait();

							Parallel.ForEach(consumers, listener =>
							{
								try
								{
									listener.StopListening();
								}
								catch (Exception ex)
								{
									Log.Error(ex, "Ошибка при остановке слушателя очереди.");
								}
							});

							Log.Information("Все слушатели очередей успешно остановлены.");
						}
						catch (Exception ex)
						{
							Log.Fatal(ex, "Критическая ошибка при остановке слушателей очередей.");
						}
					});
				}
				else
				{
					Log.Warning("Не обнаружено активных очередей для обработки. Проверьте данные в базе.");
				}
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Ошибка при запуске слушателей очередей.");
			}

			await app.RunAsync();
		}
	}
}
 