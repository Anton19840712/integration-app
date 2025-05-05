using MongoDB.Bson.Serialization.Attributes;

namespace models.dynamicgatesettings.entities
{
	[BsonIgnoreExtraElements]
	public class IncidentEntity : AuditableEntity
	{
		public string Payload { get; set; }
	}
}
