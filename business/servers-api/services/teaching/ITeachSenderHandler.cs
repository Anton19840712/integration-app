using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.models.response;

namespace servers_api.services.teaching
{
	public interface ITeachSenderHandler
	{
		Task<ResponseIntegration> TeachBPMAsync(
			CombinedModel model,
			CancellationToken token);
	}
}