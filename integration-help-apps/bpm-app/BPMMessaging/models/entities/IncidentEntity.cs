namespace BPMMessaging.models.entities
{
	public class IncidentEntity : AuditableEntity
	{
		public string InQueueName { get; set; }
		public string OutQueueName { get; set; }
		public string IncidentData { get; set; }
	}
}
