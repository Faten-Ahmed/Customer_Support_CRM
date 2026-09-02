namespace CRM.Application.Portal.DTOs;

public record PortalSurveyDto(
    Guid Id,
    string TicketNumber,
    string TicketSubject,
    DateTime SentAt,
    bool IsExpired,
    string Status);
