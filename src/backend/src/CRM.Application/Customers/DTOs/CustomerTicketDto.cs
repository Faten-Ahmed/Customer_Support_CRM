namespace CRM.Application.Customers.DTOs;

public record CustomerTicketDto(
    string TicketNumber,
    string Subject,
    string Status,
    string Priority,
    DateTime CreatedAt,
    string? Category);
