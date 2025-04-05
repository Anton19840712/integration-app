using System.Net;
using System.Net.Sockets;
using System.Text;
using servers_api.models.response;
using servers_api.messaging.sending;
using servers_api.factory;
using servers_api.validation.common;
using servers_api.models.dynamicgatesettings.internalusage;

namespace servers_api.api.streaming.udp
{
	/// <summary>
	/// UDP сервер, который продолжает отправлять сообщения после возврата ResponseIntegration.
	/// </summary>
	public class UdpServerInstance : IUpServer
	{
		private readonly ILogger<UdpServerInstance> _logger;
		private readonly IServerInstanceFluentValidator _validator;

		public UdpServerInstance(
			IMessageSender messageSender,
			IServerInstanceFluentValidator validator,
			ILogger<UdpServerInstance> logger)
		{
			_validator = validator;
			_logger = logger;
		}

		public async Task<ResponseIntegration> UpServerAsync(
			ServerInstanceModel instanceModel,
			CancellationToken cancellationToken = default)
		{
			UdpClient udp = new UdpClient(888);

			_logger.LogInformation("[Сервер] Ожидание первого сообщения от клиента...");

			// Ожидание первого сообщения от клиента
			UdpReceiveResult receiveResult = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);
			byte[] receivedBytes = receiveResult.Buffer; // Берем данные из Buffer
			string clientMessage = Encoding.UTF8.GetString(receivedBytes);
			_logger.LogInformation($"[Сервер] Получено от клиента: {clientMessage}");

			// Сохраняем адрес клиента
			IPEndPoint clientEndPoint = receiveResult.RemoteEndPoint;

			// Возвращаем успешный ответ в API
			var response = new ResponseIntegration
			{
				Message = "Соединение установлено успешно",
				Result = true
			};

			// Запускаем бесконечный цикл отправки сообщений клиенту в фоновом потоке
			_ = Task.Run(() => HandleConsoleInput(udp, clientEndPoint, cancellationToken), cancellationToken);

			return response;
		}

		private void HandleConsoleInput(UdpClient udp, IPEndPoint clientEndPoint, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				Console.Write("[Сервер] Введите сообщение для отправки клиенту: ");
				string response = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(response)) break; // Выход, если пустая строка

				byte[] responseBytes = Encoding.UTF8.GetBytes(response);
				udp.Send(responseBytes, responseBytes.Length, clientEndPoint);
				Console.WriteLine($"[Сервер] Отправлено клиенту: {response}");
			}

			Console.WriteLine("[Сервер] Завершение работы...");
		}
	}
}
