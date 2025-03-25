using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

class Program
{
	private const string Host = "127.0.0.1";
	private const int Port = 5001;

	static async Task Main(string[] args)
	{
		Console.WriteLine($"Запуск WebSocket-сервера на {Host}:{Port}...");
		HttpListener listener = new HttpListener();
		listener.Prefixes.Add($"http://{Host}:{Port}/");
		listener.Start();
		Console.WriteLine("WebSocket-сервер запущен!");

		while (true)
		{
			try
			{
				HttpListenerContext context = await listener.GetContextAsync();
				if (context.Request.IsWebSocketRequest)
				{
					Console.WriteLine("Получен новый WebSocket-запрос");
					HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
					_ = HandleConnectionAsync(wsContext.WebSocket);
				}
				else
				{
					context.Response.StatusCode = 400;
					context.Response.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при обработке запроса: {ex.Message}");
			}
		}
	}

	private static async Task HandleConnectionAsync(WebSocket webSocket)
	{
		byte[] buffer = new byte[1024];
		Console.WriteLine("Клиент подключен");

		var pingInterval = TimeSpan.FromSeconds(5); // Интервал для пинга
		var lastPingTime = DateTime.Now;

		try
		{
			while (webSocket.State == WebSocketState.Open)
			{
				// Отправка пинга раз в 5 секунд
				if ((DateTime.Now - lastPingTime) > pingInterval)
				{
					Console.WriteLine("Отправка пинга клиенту");
					await SendPingAsync(webSocket);
					lastPingTime = DateTime.Now; // Обновляем время последнего пинга
				}

				var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					Console.WriteLine("Клиент закрыл соединение");
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие соединения", CancellationToken.None);
					break;
				}
				else
				{
					string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
					Console.WriteLine($"Получено сообщение от клиента: {message}");

					// Пример ответа
					string responseMessage = $"Принято: {message}";
					byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
					await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
					Console.WriteLine($"Отправлен ответ: {responseMessage}");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка в WebSocket-соединении: {ex.Message}");
		}
		finally
		{
			Console.WriteLine("Закрытие WebSocket-соединения");
			await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сервер завершает соединение", CancellationToken.None);
		}
	}

	private static async Task SendPingAsync(WebSocket webSocket)
	{
		try
		{
			byte[] pingMessage = Encoding.UTF8.GetBytes("Ping");
			await webSocket.SendAsync(new ArraySegment<byte>(pingMessage), WebSocketMessageType.Text, true, CancellationToken.None);
			Console.WriteLine("Пинг отправлен клиенту");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка при отправке пинга: {ex.Message}");
		}
	}
}
