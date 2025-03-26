using servers_api.models.internallayer.common;
using servers_api.models.response;

namespace servers_api.Services.InternalSystems
{
	public interface ITeachSenderHandler
	{
		Task<ResponseIntegration> TeachBPMAsync(
			CombinedModel model,
			CancellationToken token);
	}
}