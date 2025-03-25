using BPMMessaging.enums;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;

namespace BPMMessaging.models.dtos
{
	public class OutboxMessage
	{
		[BsonId]
		[BsonRepresentation(BsonType.String)]
		public Guid Id { get; set; } = Guid.NewGuid();

		[BsonElement("modelType")]
		public string ModelType { get; set; }

		[BsonElement("eventType")]
		public EventTypes EventType { get; set; }

		[BsonElement("isProcessed")]
		public bool IsProcessed { get; set; }

		[BsonElement("outQueue")]
		public string OutQueue { get; set; }

		[BsonElement("inQueue")]
		public string InQueue { get; set; }

		[BsonElement("payload")]
		public BsonDocument Payload { get; set; }

		[BsonElement("createdAt")]
		[BsonRepresentation(BsonType.DateTime)]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Принудительно сохраняем в UTC
		[BsonElement("CreatedAtFormatted")]
		[BsonIgnoreIfNull]
		public string CreatedAtFormatted { get; set; }

		[BsonIgnore]
		public string FormattedDate => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

		public OutboxMessage()
		{
			CreatedAtFormatted = FormattedDate; // Заполняем перед сохранением
		}

		public string GetPayloadJson()
		{
			return Payload?.ToJson(new JsonWriterSettings()) ?? string.Empty;
		}
	}
}
