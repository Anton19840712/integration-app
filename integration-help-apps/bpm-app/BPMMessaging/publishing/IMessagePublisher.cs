using BPMMessaging.models.dtos;

namespace BPMMessaging.publishing
{
	public interface IMessagePublisher
	{
		Task PublishAsync(
			string eventType,
			OutboxMessage payload,
			CancellationToken stoppingToken);
	}
}
