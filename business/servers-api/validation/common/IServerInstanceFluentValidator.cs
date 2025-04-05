using servers_api.models.dynamicgatesettings.internalusage;
using servers_api.models.response;

namespace servers_api.validation.common
{
	public interface IServerInstanceFluentValidator
	{
		ResponseIntegration Validate(ServerInstanceModel instanceModel);
	}
}
