using System.Text.Json.Serialization;

namespace servers_api.models.dynamicgatesettings.incomingjson
{
	public record class ServerSettings : BaseConnectionSettings
	{
		[JsonPropertyName("clientHoldConnectionMs")]
		public int ClientHoldConnectionMs { get; set; }
	}
}
