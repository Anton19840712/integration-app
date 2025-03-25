namespace servers_api.constants
{
	public static class ProtocolClientConstants
	{
		// Константы для хоста и порта сервера
		public const string DefaultServerHost = "127.0.0.1";
		public const int DefaultServerPort = 6255;

		// Константы для буфера и задержки обработки сообщений
		public const int BufferSize = 1024;  // Размер буфера для чтения данных
		public const int MessageProcessingDelayMs = 500;  // Задержка при обработке сообщений (в миллисекундах)
	}
}
