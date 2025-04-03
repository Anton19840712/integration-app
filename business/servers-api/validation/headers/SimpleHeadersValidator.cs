using servers_api.models.response;

namespace servers_api.validation.headers
{
	public class SimpleHeadersValidator : IHeadersValidator
	{
		public Task<ResponseIntegration> ValidateHeadersAsync(IHeaderDictionary headers)
		{
			// Минимальная проверка: наличие X-Custom-Header
			if (!headers.ContainsKey("X-Custom-Header"))
			{
				return Task.FromResult(new ResponseIntegration
				{
					Message = "Missing required header: X-Custom-Header",
					Result = false
				});
			}

			return Task.FromResult(new ResponseIntegration
			{
				Message = "Headers are valid.",
				Result = true
			});
		}
	}
}
