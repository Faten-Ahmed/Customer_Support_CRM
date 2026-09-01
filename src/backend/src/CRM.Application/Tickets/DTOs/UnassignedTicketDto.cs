namespace CRM.Application.Tickets.DTOs;

public record UnassignedTicketDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string Subject,
    string Priority,
    string Channel,
    Guid? DepartmentId,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? ResolutionDue,
    string BreachTier);
