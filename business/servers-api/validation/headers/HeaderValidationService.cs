using servers_api.validation.headers;

public class HeaderValidationService : IHeaderValidationService
{
	private readonly IHeadersValidator _simpleValidator;
	private readonly IHeadersValidator _detailedValidator;
	private readonly ILogger<HeaderValidationService> _logger;

	public HeaderValidationService(
		SimpleHeadersValidator simpleValidator,
		DetailedHeadersValidator detailedValidator,
		ILogger<HeaderValidationService> logger)
	{
		_simpleValidator = simpleValidator ?? throw new ArgumentNullException(nameof(simpleValidator));
		_detailedValidator = detailedValidator ?? throw new ArgumentNullException(nameof(detailedValidator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<bool> ValidateHeadersAsync(IHeaderDictionary headers)
	{
		bool useDetailedValidation = headers.ContainsKey("X-Use-Detailed-Validation");
		IHeadersValidator validator = useDetailedValidation ? _detailedValidator : _simpleValidator;

		var validationResult = await validator.ValidateHeadersAsync(headers);

		if (!validationResult.Result)
		{
			_logger.LogWarning("Валидация заголовков не пройдена: {Message}", validationResult.Message);
			return false;
		}

		_logger.LogInformation("Валидация заголовков успешна.");
		return true;
	}
}
