using AutoMapper;
using BPMMessaging.models.dtos;
using BPMMessaging.models.entities;

namespace BPMMessaging.mapping
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			// Маппинг для IncidentEntity
			CreateMap<ParsedModel, IncidentEntity>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
				.ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(_ => DateTime.UtcNow));

			// Маппинг для TeachingEntity
			CreateMap<ParsedModel, TeachingEntity>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
				.ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(_ => DateTime.UtcNow));

			CreateMap<OutboxMessage, OutModel>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
				.ForMember(dest => dest.ModelType, opt => opt.MapFrom(src => src.ModelType))
				.ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType))
				.ForMember(dest => dest.IsProcessed, opt => opt.MapFrom(src => src.IsProcessed))
				.ForMember(dest => dest.OutQueue, opt => opt.MapFrom(src => src.OutQueue))
				.ForMember(dest => dest.InQueue, opt => opt.MapFrom(src => src.InQueue))
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
				.ForMember(dest => dest.CreatedAtFormatted, opt => opt.MapFrom(src => src.CreatedAtFormatted))
				.ForMember(dest => dest.FormattedDate, opt => opt.MapFrom(src => src.FormattedDate))
				.ConstructUsing(src => new OutModel
				{
					Id = src.Id,
					ModelType = src.ModelType,
					EventType = src.EventType,
					IsProcessed = src.IsProcessed,
					OutQueue = src.OutQueue,
					InQueue = src.InQueue,
					CreatedAt = src.CreatedAt,
					CreatedAtFormatted = src.CreatedAtFormatted,
					FormattedDate = src.FormattedDate,
					PayloadId = src.Payload.Contains("Id") ? src.Payload["Id"].AsString : null // Вынесли логику сюда
				});
		}
	}
}
