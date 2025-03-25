using MongoDB.Bson.Serialization.Attributes;

namespace servers_api.models.entities
{
	[BsonIgnoreExtraElements]
	public class QueuesEntity : AuditableEntity
	{
		[BsonElement("inQueueName")] // Указываем точное имя поля в MongoDB
		public string InQueueName { get; set; }

		[BsonElement("outQueueName")] // Указываем точное имя поля в MongoDB
		public string OutQueueName { get; set; }
	}
}
