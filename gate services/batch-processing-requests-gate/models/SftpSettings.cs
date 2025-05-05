namespace sftp_dynamic_gate_app.models
{
	public class SftpSettings
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string Source { get; set; }
	}
}
