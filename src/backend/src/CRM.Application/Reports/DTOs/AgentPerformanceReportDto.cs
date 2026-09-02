namespace CRM.Application.Reports.DTOs;

public record AgentPerformanceDto(
    Guid AgentId,
    string AgentName,
    int TicketsHandled,
    int TicketsResolved,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    decimal SlaComplianceRate,
    decimal? CsatScore,
    int CsatResponseCount,
    decimal EscalationRate);
