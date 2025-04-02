using Microsoft.Extensions.DependencyInjection;

namespace servers_api.validation.headers
{
	// Сервис для валидации заголовков
	public class HeaderValidationService : IHeaderValidationService
	{
		private readonly IHeadersValidator _simpleValidator;
		private readonly IHeadersValidator _detailedValidator;
		private readonly ILogger<HeaderValidationService> _logger;

		public HeaderValidationService(
			IServiceProvider serviceProvider,
			IHeadersValidator simpleValidator,
			IHeadersValidator detailedValidator,
			ILogger<HeaderValidationService> logger)
		{
			try
			{
				_logger = logger ?? throw new ArgumentNullException(nameof(logger));

				// Получаем конкретные реализации по типам
				_simpleValidator = serviceProvider.GetService<SimpleHeadersValidator>();
				if (_simpleValidator == null)
				{
					_logger.LogWarning("SimpleHeadersValidator не был зарегистрирован.");
				}

				_detailedValidator = serviceProvider.GetService<DetailedHeadersValidator>();
				if (_detailedValidator == null)
				{
					_logger.LogWarning("DetailedHeadersValidator не был зарегистрирован.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Ошибка при инъекции сервисов валидации: {ex.Message}");
				_simpleValidator = null;
				_detailedValidator = null;
			}
		}

		public async Task<bool> ValidateHeadersAsync(IHeaderDictionary headers)
		{
			var useDetailedValidation = headers.ContainsKey("X-Use-Detailed-Validation");
			var validator = useDetailedValidation ? _detailedValidator : _simpleValidator;
			var validationResult = validator.ValidateHeaders(headers);

			if (!validationResult.Result)
			{
				_logger.LogWarning("Валидация заголовков не пройдена.");
				await Task.Delay(0);
				return false;
			}

			_logger.LogInformation("Валидация заголовков успешна.");
			return true;
		}
	}
}
