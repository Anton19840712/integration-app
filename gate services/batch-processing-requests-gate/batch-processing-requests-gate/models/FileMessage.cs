namespace sftp_dynamic_gate_app.models
{
	public class FileMessage
	{
		public byte[] FileContent { get; set; }
		public string FileName { get; set; } // Добавлено поле для имени файла
	}
}
