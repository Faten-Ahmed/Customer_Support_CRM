namespace CRM.Application.Agents.DTOs;

public record MyTicketDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerFullName,
    string Subject,
    string Status,
    string Priority,
    string Channel,
    Guid? DepartmentId,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? ResolutionDue,
    string SlaStatus,
    int? ResolutionRemainingMinutes);
