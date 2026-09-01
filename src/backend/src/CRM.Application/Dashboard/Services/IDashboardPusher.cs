namespace CRM.Application.Dashboard.Services;

public interface IDashboardPusher
{
    Task ScheduleKpiPushAsync(Guid departmentId, CancellationToken ct = default);
    Task ScheduleWorkloadPushAsync(Guid departmentId, CancellationToken ct = default);
}
