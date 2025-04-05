using System.Text.Json;
using servers_api.models.dynamicgatesettings.internalusage;

namespace servers_api.Services.Parsers
{
	/// <summary>
	/// Парсер входящей информации.
	/// </summary>
	public interface IJsonParsingService
	{
		Task<CombinedModel> ParseJsonAsync(
			JsonElement jsonBody,
			bool isTeaching,
			CancellationToken stoppingToken);
	}
}
