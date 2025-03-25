using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConsoleApp1
{
	class Program
	{
		static UdpClient udp = new UdpClient();
		static bool errorLogged = false; // Флаг для логирования ошибки один раз

		static void Main(string[] args)
		{
			Console.Title = "Client";
			udp.Connect("127.0.0.1", 888);

			IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
			// Отправка сообщения
			string message = "Привет, сервер!";
			byte[] sendByte = Encoding.UTF8.GetBytes(message);
			udp.Send(sendByte, sendByte.Length);
			Console.WriteLine($"[Клиент] Отправлено: {message}");


			while (true)
			{
				try
				{
					// Ожидание ответа от сервера
					byte[] responseBytes = udp.Receive(ref serverEndPoint);
					string response = Encoding.UTF8.GetString(responseBytes);
					Console.WriteLine($"[Клиент] Получен ответ от сервера: {response}");

					// Сбрасываем флаг, если соединение успешно
					errorLogged = false;

					// Ждем перед следующей отправкой
					Thread.Sleep(2000);
				}
				catch (Exception ex)
				{
					// Логируем ошибку только один раз
					if (!errorLogged)
					{
						Console.WriteLine($"[Клиент] Ошибка: {ex.Message}");
						errorLogged = true;
					}

					// Пауза перед повторной попыткой
					Thread.Sleep(2000);
				}
			}
		}
	}
}
