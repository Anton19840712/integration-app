using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BPMMessaging.models.entities
{
	[BsonIgnoreExtraElements]
	public class TeachingEntity : AuditableEntity
	{
		public string InQueueName { get; set; }
		public string OutQueueName { get; set; }
		public BsonDocument IncomingModel { get; set; }
	}
}
