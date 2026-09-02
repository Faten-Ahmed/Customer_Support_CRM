namespace CRM.Application.Tickets.DTOs;

public record TicketMessageDto(
    Guid Id,
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid? AuthorUserId,
    string? AuthorName,
    Guid? AuthorCustomerId,
    DateTime CreatedAt);
