using System.Text.Json;

namespace BPMMessaging.parsing
{
	public interface IJsonParsingService
	{
		T ParseJson<T>(JsonElement jsonBody) where T : class;
	}
}
