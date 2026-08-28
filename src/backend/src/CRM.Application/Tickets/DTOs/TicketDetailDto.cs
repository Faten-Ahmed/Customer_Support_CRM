namespace CRM.Application.Tickets.DTOs;

public record TicketDetailDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string Description,
    string Status,
    string Priority,
    string Channel,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string? CategoryName,
    string? DepartmentName,
    string? CustomFieldValues,
    SlaInfoDto? Sla,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt);

public record SlaInfoDto(
    DateTime? FirstResponseDue,
    DateTime? ResolutionDue,
    bool FirstResponseBreached,
    bool ResolutionBreached,
    string BreachTier);
