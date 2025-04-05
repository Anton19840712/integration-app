using AutoMapper;
using servers_api.api.streaming.tcp;
using servers_api.api.streaming.udp;
using servers_api.factory;
using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.models.response;

public class ProtocolManager : IProtocolManager
{
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<ProtocolManager> _logger;
	private readonly IMapper _mapper;

	public ProtocolManager(
		IServiceProvider serviceProvider,
		ILogger<ProtocolManager> logger,
		IMapper mapper)
	{
		this.serviceProvider = serviceProvider;
		_logger = logger;
		_mapper = mapper;
	}

	public async Task<ResponseIntegration> UpNodeAsync(
		CombinedModel parsedModel,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"Запуск ConfigureAsync метода с протоколом: {Protocol}, роль: Сервер - {IsServer}, Клиент - {IsClient}",
			parsedModel.Protocol,
			parsedModel.DataOptions.IsServer,
			parsedModel.DataOptions.IsClient);

		InstanceModel instanceModel = parsedModel.DataOptions.IsClient
			? _mapper.Map<ClientInstanceModel>(parsedModel)
			: _mapper.Map<ServerInstanceModel>(parsedModel);


		// Динамическое получение фабрики на основе протокола
		UpInstanceByProtocolFactory factory = parsedModel.Protocol.ToLower() switch
		{
			"tcp" => serviceProvider.GetService<TcpFactory>(),  // Убираем необходимость использования GetRequiredService
			"udp" => serviceProvider.GetService<UdpFactory>(),
			"ws" => serviceProvider.GetService<WebSocketFactory>(),
			_ => null
		};

		if (factory == null)
		{
			_logger.LogError("Неизвестный протокол: {Protocol}", parsedModel.Protocol);
		}

		if (instanceModel is ClientInstanceModel clientModel)
		{
			return await ConfigureClientAsync(clientModel, factory);
		}
		else if (instanceModel is ServerInstanceModel serverModel)
		{
			return await ConfigureServerAsync(serverModel, factory);
		}

		return new ResponseIntegration
		{
			Message = "Настройка протокола не была завершена успешно.",
			Result = false
		};
	}

	private async Task<ResponseIntegration> ConfigureClientAsync(
		ClientInstanceModel clientModel,
		UpInstanceByProtocolFactory factory)
	{
		var client = factory.CreateClient();
		var serverHost = clientModel.ServerHostPort.Host;
		var serverPort = clientModel.ServerHostPort.Port ?? 80;

		_logger.LogInformation("Настройка клиента {Protocol} для подключения к серверу {Host}:{Port}",
			clientModel.Protocol, serverHost, serverPort);

		using var cts = new CancellationTokenSource(clientModel.ClientConnectionSettings.ConnectionTimeoutMs);
		return await client.ConnectToServerAsync(clientModel, serverHost, serverPort, cts.Token);
	}

	private async Task<ResponseIntegration> ConfigureServerAsync(
		ServerInstanceModel serverModel,
		UpInstanceByProtocolFactory factory)
	{
		var server = factory.CreateServer();
		_logger.LogInformation("Запуск сервера {Protocol} на {Host}:{Port}",
			serverModel.Protocol, serverModel.Host, serverModel.Port);

		using var cts = new CancellationTokenSource(serverModel.ServerConnectionSettings.BusIdleTimeoutMs);
		return await server.UpServerAsync(serverModel, cts.Token);
	}
}
