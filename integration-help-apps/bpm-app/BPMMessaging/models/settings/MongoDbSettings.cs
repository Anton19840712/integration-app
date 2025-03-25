namespace BPMMessaging.models.settings
{
	public class MongoDbSettings
	{
		public string ConnectionString { get; set; }
		public string DatabaseName { get; set; }
		public MongoDbCollections Collections { get; set; }
	}

	public class MongoDbCollections
	{
		public string IncidentCollection { get; set; }
		public string TeachingCollection { get; set; }
		public string OutboxMessages { get; set; }
	}
}
