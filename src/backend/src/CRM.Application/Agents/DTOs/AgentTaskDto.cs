namespace CRM.Application.Agents.DTOs;

public record AgentTaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    DateTime? DueAt,
    bool IsOverdue,
    Guid? TicketId,
    Guid? CustomerId,
    DateTime CreatedAt,
    DateTime? CompletedAt);
