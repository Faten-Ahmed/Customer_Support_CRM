namespace CRM.Application.Tickets.DTOs;

public record AttachmentDto(
    Guid Id,
    Guid TicketId,
    Guid? MessageId,
    string FileName,
    string ContentType,
    long FileSize,
    string? PresignedUrl,
    string? UploaderName,
    DateTime UploadedAt);
