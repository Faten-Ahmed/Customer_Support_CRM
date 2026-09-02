namespace CRM.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
