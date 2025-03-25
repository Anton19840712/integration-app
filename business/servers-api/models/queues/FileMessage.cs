namespace servers_api.models.queues
{
	public class FileMessage
	{
		public byte[] FileContent { get; set; }
		public string FileName { get; set; } // Добавлено поле для имени файла
	}
}
