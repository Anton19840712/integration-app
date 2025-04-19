using servers_api.models.dynamicgatesettings.internalusage;

public interface IJsonParsingService
{
	Task<CombinedModel> ParseFromConfigurationAsync(CancellationToken stoppingToken);
}