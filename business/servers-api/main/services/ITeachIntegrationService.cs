using System.Text.Json;
using servers_api.models.response;

namespace servers_api.main.services;

public interface ITeachIntegrationService
{
	Task<List<ResponseIntegration>> TeachAsync(
		JsonElement jsonBody,
		CancellationToken stoppingToke);
}
