namespace CRM.Application.Reports.DTOs;

public record SlaComplianceReportDto(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    Dictionary<string, SlaComplianceByPriorityDto> ByPriority,
    SlaBreachReasonsDto BreachReasons);

public record SlaComplianceByPriorityDto(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    int TotalTickets);

public record SlaBreachReasonsDto(int WarningCount, int BreachCount, int CriticalBreachCount);
