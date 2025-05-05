using models.dynamicgatesettings.internalusage;
using models.response;

namespace validation.common
{
	public interface IServerInstanceFluentValidator
	{
		ResponseIntegration Validate(ServerInstanceModel instanceModel);
	}
}
