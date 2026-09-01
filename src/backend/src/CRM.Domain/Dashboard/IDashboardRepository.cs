namespace CRM.Domain.Dashboard;

public record AgentWorkloadData(
    Guid AgentId, string AgentName, int OpenTickets, string AvailabilityStatus);

public record DashboardKpiData(
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
    List<AgentWorkloadData> AgentWorkload,
    DateTime CalculatedAt);

public interface IDashboardRepository
{
    Task<DashboardKpiData> GetKpisAsync(
        IReadOnlyList<Guid>? departmentIds,
        Guid? agentId,
        CancellationToken ct = default);
}
