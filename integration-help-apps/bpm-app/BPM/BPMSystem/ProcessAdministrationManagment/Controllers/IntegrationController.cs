using AutoMapper;
using BPMMessaging.models.dtos;
using BPMMessaging.models.entities;
using BPMMessaging.parsing;
using BPMMessaging.repository;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[Route("Integration")]
[ApiController]
public class IntegrationController : ControllerBase
{
	private readonly IMongoRepository<TeachingEntity> _teachingRepository;
	private readonly IMongoRepository<OutboxMessage> _outboxRepository;
	private readonly IJsonParsingService _jsonParsingService;

	public IntegrationController(
		IMongoRepository<TeachingEntity> teachingRepository,
		IMongoRepository<OutboxMessage> outboxRepository,
		IJsonParsingService jsonParsingService,
		IMapper mapper)
	{
		_teachingRepository = teachingRepository;
		_outboxRepository = outboxRepository;
		_jsonParsingService = jsonParsingService;
	}

	[HttpPost("save")]
	public async Task<ActionResult> SaveTeachingModelAsync([FromBody] JsonElement model)
	{
		try
		{
			// 1. Парсим входящую модель:
			var parsedModel = _jsonParsingService.ParseJson<TeachingEntity>(model);

			// 2. Проверяем, есть ли уже такая модель в БД
			var existingModel = (await _teachingRepository.FindAsync(x =>
				x.InQueueName == parsedModel.InQueueName &&
				x.OutQueueName == parsedModel.OutQueueName)).FirstOrDefault();

			if (existingModel != null)
			{
				parsedModel.Id = existingModel.Id; // Сохраняем ID:
				await _teachingRepository.UpdateAsync(existingModel.Id, parsedModel);
			}
			else
			{
				// Если модели нет — вставляем новую:
				await _teachingRepository.InsertAsync(parsedModel);
			}

			// 3. Создаем событие для OutboxMessage
			var outboxMessage = new OutboxMessage
			{
				InQueue = parsedModel.InQueueName,
				OutQueue = parsedModel.OutQueueName,
				ModelType = "teaching",
				Payload = parsedModel.IncomingModel,
				IsProcessed = false
			};

			await _outboxRepository.InsertAsync(outboxMessage);

			return Ok(new { Message = "TeachingEntity успешно сохранена" });
		}
		catch (Exception ex)
		{
			return BadRequest(new { Error = "Ошибка при обработке модели", Details = ex.Message });
		}
	}
}
