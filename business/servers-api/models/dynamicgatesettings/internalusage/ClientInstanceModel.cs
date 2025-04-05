using servers_api.models.dynamicgatesettings.incomingjson;

namespace servers_api.models.dynamicgatesettings.internalusage
{
	/// <summary>
	/// Модель для клиента
	/// </summary>
	public class ClientInstanceModel : InstanceModel
	{
		public string ClientHost { get; set; }
		public int ClientPort { get; set; }
		public ClientSettings ClientConnectionSettings { get; set; }
		public ConnectionEndpoint ServerHostPort { get; set; }
	}
}
