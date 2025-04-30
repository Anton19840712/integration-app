using MongoDB.Bson.Serialization.Attributes;

namespace servers_api.models.dynamicgatesettings.entities
{
	[BsonIgnoreExtraElements]
	public class QueuesEntity : AuditableEntity
	{
		[BsonElement("inQueueName")]
		public string InQueueName { get; set; }

		[BsonElement("outQueueName")]
		public string OutQueueName { get; set; }
	}
}
