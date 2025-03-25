using BPMMessaging.enums;

namespace BPMMessaging.models.dtos
{
	public class OutModel
	{
		public Guid Id { get; set; }
		public string ModelType { get; set; }
		public EventTypes EventType { get; set; }
		public bool IsProcessed { get; set; }
		public string OutQueue { get; set; }
		public string InQueue { get; set; }

		// public string Payload { get; set; }
		public string PayloadId { get; set; }
		public DateTime CreatedAt { get; set; }
		public string CreatedAtFormatted { get; set; }
		public string FormattedDate { get; set; }
	}
}
