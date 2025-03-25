using MongoDB.Bson.Serialization.Attributes;

namespace servers_api.models.entities
{
	[BsonIgnoreExtraElements]
	public class IncidentEntity : AuditableEntity
	{
		public string Payload { get; set; }
	}
}
