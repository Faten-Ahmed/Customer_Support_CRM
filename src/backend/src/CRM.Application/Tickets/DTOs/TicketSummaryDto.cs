namespace CRM.Application.Tickets.DTOs;

public record TicketSummaryDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string Status,
    string Priority,
    string Channel,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
