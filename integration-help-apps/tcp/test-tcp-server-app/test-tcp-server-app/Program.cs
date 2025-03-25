using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
	const int Port = 6254;

	static async Task Main()
	{
		Console.Title = "outside server";
		var listener = new TcpListener(IPAddress.Any, Port);
		listener.Start();
		Console.WriteLine($"Сервер запущен на порту {Port}");

		while (true)
		{
			TcpClient client = await listener.AcceptTcpClientAsync();
			Console.WriteLine("Клиент подключен");
			_ = Task.Run(() => HandleClientAsync(client));
		}
	}

	static async Task HandleClientAsync(TcpClient client)
	{
		try
		{
			using (client)
			using (var stream = client.GetStream())
			{
				int messageCount = 1;
				byte[] buffer = new byte[1024];

				while (client.Connected)
				{
					// Проверяем, жив ли клиент
					if (stream.DataAvailable)
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
						if (bytesRead == 0) break; // Клиент закрыл соединение
						Console.WriteLine($"Получено сообщение: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
					}

					string message = $"Test message {messageCount}";
					byte[] data = Encoding.UTF8.GetBytes(message);
					await stream.WriteAsync(data, 0, data.Length);
					Console.WriteLine($"Отправлено сообщение номер {messageCount}");

					messageCount++;
					await Task.Delay(2000);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
		}
		finally
		{
			Console.WriteLine("Клиент отключился");
		}
	}
}
