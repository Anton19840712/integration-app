namespace servers_api.validation.headers
{
	// Интерфейс для сервиса валидации заголовков:
	public interface IHeaderValidationService
	{
		Task<bool> ValidateHeadersAsync(IHeaderDictionary headers);
	}
}
