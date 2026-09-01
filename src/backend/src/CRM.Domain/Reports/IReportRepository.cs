namespace CRM.Domain.Reports;

public record TicketVolumeData(
    int TotalCreated, int TotalResolved, int TotalClosed, int OpenAtEndOfPeriod,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByPriority,
    Dictionary<string, int> ByChannel,
    List<TrendPoint> Trend);

public record TrendPoint(string Date, int Created, int Resolved);

public record SlaComplianceByPriority(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    int TotalTickets);

public record SlaBreachReasons(int WarningCount, int BreachCount, int CriticalBreachCount);

public record SlaComplianceData(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    Dictionary<string, SlaComplianceByPriority> ByPriority,
    SlaBreachReasons BreachReasons);

public record AgentPerformanceData(
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

public record CsatOverallData(
    decimal? AvgRating, int TotalSent, int TotalSubmitted, decimal ResponseRate);

public record CsatByDepartmentData(
    Guid DepartmentId, string DepartmentName, decimal? AvgRating, int TotalSubmitted);

public record CsatByAgentData(
    Guid AgentId, string AgentName, decimal? AvgRating, int TotalSubmitted);

public record CsatReportData(
    CsatOverallData Overall,
    Dictionary<int, int> Distribution,
    List<CsatByDepartmentData> ByDepartment,
    List<CsatByAgentData> ByAgent,
    List<string> RecentComments);

public interface IReportRepository
{
    Task<TicketVolumeData> GetTicketVolumeAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        string groupBy,
        CancellationToken ct = default);

    Task<SlaComplianceData> GetSlaComplianceAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        string? priority,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentPerformanceData>> GetAgentPerformanceAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        CancellationToken ct = default);

    Task<CsatReportData> GetCsatReportAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        CancellationToken ct = default);

    Task<int> CountRowsAsync(
        string reportType,
        IReadOnlyList<Guid>? departmentIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default);
}
