using System.Net;
using System.Text;
using servers_api.factory;
using servers_api.models.internallayer.instance;
using servers_api.models.response;

namespace servers_api.protocols.http
{
	public class HttpServerInstance : IUpServer
	{
		private readonly ILogger<HttpServerInstance> _logger;
		private readonly HttpListener _listener;
		private bool _isRunning;
		private string _host;
		private int _port;

		public HttpServerInstance(ILogger<HttpServerInstance> logger)
		{
			_logger = logger;
			_listener = new HttpListener();
		}

		public async Task<ResponseIntegration> UpServerAsync(
			ServerInstanceModel instanceModel,
			CancellationToken cancellationToken)
		{
			try
			{
				_host = instanceModel.Host;
				_port = instanceModel.Port;
				_listener.Prefixes.Clear();
				_listener.Prefixes.Add($"http://{_host}:{_port}/");

				_logger.LogInformation("Запуск HTTP-сервера на {Host}:{Port}...", _host, _port);
				await StartAsync(cancellationToken);

				return new ResponseIntegration
				{
					Message = $"HTTP сервер успешно запущен на {_host}:{_port}",
					Result = true
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка запуска HTTP-сервера.");
				return new ResponseIntegration
				{
					Message = $"Ошибка запуска сервера: {ex.Message}",
					Result = false
				};
			}
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_listener.Start();
			_isRunning = true;
			_logger.LogInformation("HTTP сервер запущен на {Host}:{Port}", _host, _port);

			while (!cancellationToken.IsCancellationRequested && _isRunning)
			{
				try
				{
					var context = await _listener.GetContextAsync();
					_logger.LogInformation("Принят HTTP-запрос от {0}", context.Request.RemoteEndPoint);

					_ = Task.Run(() => HandleClientAsync(context), cancellationToken);
				}
				catch (Exception ex) when (_isRunning)
				{
					_logger.LogError(ex, "Ошибка в HTTP сервере");
				}
			}
		}

		private async Task HandleClientAsync(HttpListenerContext context)
		{
			_logger.LogInformation("Принят HTTP-запрос от {RemoteEndPoint}", context.Request.RemoteEndPoint);

			var response = context.Response;
			var responseString = "Привет от сервера!";
			var buffer = Encoding.UTF8.GetBytes(responseString);

			response.ContentLength64 = buffer.Length;
			using var output = response.OutputStream;
			await output.WriteAsync(buffer, 0, buffer.Length);

			_logger.LogInformation("Отправлен ответ клиенту: {ResponseString}", responseString);
			_logger.LogInformation("Соединение с клиентом {RemoteEndPoint} установлено успешно", context.Request.RemoteEndPoint);
		}


		public void Stop()
		{
			_isRunning = false;
			_listener.Stop();
			_logger.LogInformation("HTTP сервер остановлен.");
		}
	}
}
