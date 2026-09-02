namespace CRM.Application.Dashboard.DTOs;

public record DashboardKpiDto(
    int OpenTickets,
    Dictionary<string, int> OpenByPriority,
    decimal SlaBreachRate,
    decimal AvgFirstResponseMinutes7Day,
    decimal AvgResolutionMinutes7Day,
    decimal? CsatScore30Day,
    decimal AgentUtilization,
    int TicketsTodayCreated,
    int TicketsTodayResolved,
    decimal EscalationRate,
    int UnassignedTickets,
    IReadOnlyList<AgentWorkloadDto>? AgentWorkload,
    DateTime CalculatedAt);

public record AgentWorkloadDto(
    Guid AgentId, string AgentName, int OpenTickets, string AvailabilityStatus);
