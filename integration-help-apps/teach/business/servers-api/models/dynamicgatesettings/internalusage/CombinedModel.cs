﻿namespace servers_api.models.dynamicgatesettings.internalusage
{
	/// <summary>
	/// Модель для пересылки на bpm для ее обучения работе с новыми структурами данных.
	/// </summary>
	public class CombinedModel
	{
		public string Id { get; set; }
		public string InQueueName { get; set; }
		public string OutQueueName { get; set; }
		public string Protocol { get; set; }
		public string InternalModel { get; set; }
		public string DataFormat { get; set; }
	}
}
