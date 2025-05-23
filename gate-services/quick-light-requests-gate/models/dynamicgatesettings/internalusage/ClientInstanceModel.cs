﻿using models.dynamicgatesettings.incomingjson;

namespace models.dynamicgatesettings.internalusage
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
