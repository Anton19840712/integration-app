﻿namespace servers_api.models.response
{
	/// <summary>
	/// Модель ответов с результатами сервисов по настройке динамического шлюза. 
	/// </summary>
	public class ResponseIntegration
	{
		public string Message { get; set; }
		public bool Result { get; set; }
	}
}
