using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.models.response;

namespace servers_api.factory
{
	public interface IProtocolManager
	{
		Task<ResponseIntegration> UpNodeAsync(
			CombinedModel combinedModel,
			CancellationToken stoppingToken);
	}
}
