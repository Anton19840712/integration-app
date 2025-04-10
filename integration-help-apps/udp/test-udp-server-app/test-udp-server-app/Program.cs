using System.Net.Sockets;
using System.Text;

namespace UdpServerApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 5018;
            var udpServer = new UdpServer(port);

            Console.WriteLine($"Запуск UDP-сервера на порту {port}...");
            var cts = new CancellationTokenSource();

            var serverTask = udpServer.StartAsync(cts.Token);

            Console.WriteLine("Нажмите любую клавишу для остановки сервера.");
            Console.ReadKey();

            cts.Cancel();
            await serverTask;

            Console.WriteLine("Сервер остановлен.");
        }
    }

    public class UdpServer
    {
        private readonly int _port;
        private UdpClient _udpServer;
        private CancellationTokenSource _cts;
        private DateTime _lastClientMessageTime;

        public UdpServer(int port)
        {
            _port = port;
            _lastClientMessageTime = DateTime.UtcNow;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _udpServer = new UdpClient(_port);

            Console.WriteLine($"UDP-сервер запущен на порту {_port}.");

			try
			{
				Console.WriteLine("Ожидание первого сообщения от клиента...");

				// Получаем первое сообщение от клиента
				var result = await _udpServer.ReceiveAsync();
				_lastClientMessageTime = DateTime.UtcNow;

				string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
				Console.WriteLine($"Получено сообщение от клиента: {receivedMessage}");

				var clientEndPoint = result.RemoteEndPoint;

				// После первого сообщения начинаем бесконечно слать данные
				int messageCounter = 1;
				while (!_cts.Token.IsCancellationRequested)
				{
					string messageToSend = $"Сообщение {messageCounter++} от UDP-сервера";
					byte[] sendBytes = Encoding.UTF8.GetBytes(messageToSend);
					await _udpServer.SendAsync(sendBytes, sendBytes.Length, clientEndPoint);

					Console.WriteLine($"Отправлено сообщение: {messageToSend}");

					// Ждём ответ от клиента с таймаутом
					var receiveTask = _udpServer.ReceiveAsync();
					var timeoutTask = Task.Delay(5000);

					var completed = await Task.WhenAny(receiveTask, timeoutTask);
					if (completed == receiveTask)
					{
						var clientResponse = await receiveTask;
						string response = Encoding.UTF8.GetString(clientResponse.Buffer);
						Console.WriteLine($"Получен ответ от клиента: {response}");
					}
					else
					{
						Console.WriteLine("Клиент не ответил на сообщение.");
					}

					await Task.Delay(3000, _cts.Token);
				}
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("Сервер остановлен по запросу.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка в UDP-сервере: {ex.Message}");
			}
            finally
            {
                _udpServer?.Close();
                Console.WriteLine("Сервер завершил работу.");
            }
        }
    }
}
