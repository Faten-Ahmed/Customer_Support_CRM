namespace CRM.Application.Chat.DTOs;

public record ChatMessageDto(
    Guid Id,
    Guid SessionId,
    string SenderRole,
    Guid? SenderId,
    string Body,
    DateTime SentAt);
