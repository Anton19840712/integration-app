using System.Text.Json.Serialization;

namespace models.dynamicgatesettings.incomingjson
{
	public record class ServerSettings : BaseConnectionSettings
	{
		[JsonPropertyName("clientHoldConnectionMs")]
		public int ClientHoldConnectionMs { get; set; }
	}
}
