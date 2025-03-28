﻿using servers_api.models.response;

namespace servers_api.validation.headers
{
	public class DetailedHeadersValidator : IHeadersValidator
	{
		private readonly ILogger<DetailedHeadersValidator> _logger;

		public DetailedHeadersValidator(ILogger<DetailedHeadersValidator> logger)
		{
			_logger = logger;
		}

		public ResponseIntegration ValidateHeaders(IHeaderDictionary headers)
		{
			var errors = new List<string>();

			// 1. Проверяем наличие заголовка Authorization
			if (!headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
			{
				errors.Add("Missing Authorization header");
			}
			else if (!authHeader.ToString().StartsWith("Bearer "))
			{
				errors.Add("Authorization header must start with 'Bearer '");
			}

			// 2. Проверяем Content-Type
			if (!headers.TryGetValue("Content-Type", out var contentType) || contentType != "application/json")
			{
				errors.Add("Invalid or missing Content-Type header. Expected 'application/json'");
			}

			// 3. Проверяем кастомный заголовок (если нужно)
			if (!headers.TryGetValue("X-Custom-Header", out var customHeader))
			{
				errors.Add("Missing X-Custom-Header");
			}

			// Если ошибки есть — логируем и возвращаем отрицательный ответ
			if (errors.Any())
			{
				foreach (var error in errors)
				{
					_logger.LogError(error);
				}

				return new ResponseIntegration
				{
					Message = string.Join("; ", errors),
					Result = false
				};
			}

			return new ResponseIntegration
			{
				Message = "Headers validation passed",
				Result = true
			};
		}
	}
}
