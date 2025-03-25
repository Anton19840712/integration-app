using System.Net.Sockets;
using System.Text;

namespace UdpServerApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 6255;
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
                while (!_cts.Token.IsCancellationRequested)
                {
                    var receiveTask = _udpServer.ReceiveAsync();
                    var delayTask = Task.Delay(10000); // Ожидание нового сообщения 10 секунд

                    var completedTask = await Task.WhenAny(receiveTask, delayTask);
                    if (completedTask == delayTask)
                    {
                        Console.WriteLine("Клиент не отвечает. Завершаем работу.");
                        break;
                    }

                    var result = await receiveTask;
                    _lastClientMessageTime = DateTime.UtcNow;

                    string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                    Console.WriteLine($"Получено сообщение от клиента: {receivedMessage}");

                    for (int i = 0; i < 10; i++)
                    {
                        string messageToSend = $"Сообщение {i + 1} от UDP-сервера";
                        byte[] sendBytes = Encoding.UTF8.GetBytes(messageToSend);
                        await _udpServer.SendAsync(sendBytes, sendBytes.Length, result.RemoteEndPoint);

                        Console.WriteLine($"Отправлено сообщение: {messageToSend}");
                        _lastClientMessageTime = DateTime.UtcNow; // ОБНОВЛЯЕМ таймстамп при отправке

                        var confirmTask = _udpServer.ReceiveAsync(); // Ждем подтверждение от клиента
                        var timeoutTask = Task.Delay(5000); // 5 секунд на подтверждение

                        var response = await Task.WhenAny(confirmTask, timeoutTask);
                        if (response == timeoutTask)
                        {
                            Console.WriteLine("Клиент не ответил на сообщение. Останавливаемся.");
                            return;
                        }

                        await Task.Delay(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                _udpServer?.Close();
                Console.WriteLine("Сервер завершил работу.");
            }
        }
    }
}
