
using System.Net.WebSockets;
using System.Text;

class WebSocketClient
{
	public static async Task Main(string[] args)
	{
		string host = "localhost";
		int port = 6254;

		using (var client = new ClientWebSocket())
		{
			try
			{
				var uri = new Uri($"ws://{host}:{port}/ws");
				Console.WriteLine($"Подключение к {uri}...");

				await client.ConnectAsync(uri, CancellationToken.None);
				Console.WriteLine("Подключено к серверу!");

				// Отправка сообщения серверу
				string message = "Привет, сервер!";
				await SendMessageAsync(client, message);

				// Чтение ответа от сервера
				await ReceiveMessagesAsync(client);

				// Закрытие соединения
				await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Завершение работы", CancellationToken.None);
				Console.WriteLine("Соединение закрыто.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка: {ex.Message}");
			}
		}
	}

	private static async Task SendMessageAsync(ClientWebSocket client, string message)
	{
		byte[] buffer = Encoding.UTF8.GetBytes(message);
		await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
		Console.WriteLine($"Отправлено: {message}");
	}

	private static async Task ReceiveMessagesAsync(ClientWebSocket client)
	{
		byte[] buffer = new byte[4096];

		while (client.State == WebSocketState.Open)
		{
			var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

			if (result.MessageType == WebSocketMessageType.Close)
			{
				Console.WriteLine("Сервер закрыл соединение.");
				await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие клиентом", CancellationToken.None);
			}
			else
			{
				var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				Console.WriteLine($"Получено: {message}");
			}
		}
	}
}
