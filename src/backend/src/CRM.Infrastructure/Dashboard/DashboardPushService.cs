using CRM.Application.Dashboard.Services;
using CRM.Domain.Dashboard;
using CRM.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CRM.Infrastructure.Dashboard;

public class DashboardPushService : IDashboardPusher
{
    private readonly IHubContext<DashboardHub> _hub;
    private readonly IDashboardRepository _dashboard;
    private readonly Dictionary<Guid, System.Timers.Timer> _kpiTimers = new();
    private readonly Dictionary<Guid, System.Timers.Timer> _workloadTimers = new();
    private readonly object _lock = new();

    public DashboardPushService(
        IHubContext<DashboardHub> hub,
        IDashboardRepository dashboard)
    {
        _hub = hub;
        _dashboard = dashboard;
    }

    public Task ScheduleKpiPushAsync(Guid departmentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_kpiTimers.TryGetValue(departmentId, out var existing))
            {
                existing.Stop();
                existing.Start();
                return Task.CompletedTask;
            }

            var timer = new System.Timers.Timer(2000) { AutoReset = false };
            timer.Elapsed += async (_, _) => await PushKpiAsync(departmentId);
            _kpiTimers[departmentId] = timer;
            timer.Start();
        }
        return Task.CompletedTask;
    }

    public Task ScheduleWorkloadPushAsync(Guid departmentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_workloadTimers.TryGetValue(departmentId, out var existing))
            {
                existing.Stop();
                existing.Start();
                return Task.CompletedTask;
            }

            var timer = new System.Timers.Timer(2000) { AutoReset = false };
            timer.Elapsed += async (_, _) => await PushWorkloadAsync(departmentId);
            _workloadTimers[departmentId] = timer;
            timer.Start();
        }
        return Task.CompletedTask;
    }

    private async Task PushKpiAsync(Guid departmentId)
    {
        lock (_lock) _kpiTimers.Remove(departmentId);
        var data = await _dashboard.GetKpisAsync(new[] { departmentId }, null);
        await _hub.Clients.Group("kpi-admin").SendAsync("KpiUpdated", data);
        await _hub.Clients.Group("kpi-manager").SendAsync("KpiUpdated", data);
    }

    private async Task PushWorkloadAsync(Guid departmentId)
    {
        lock (_lock) _workloadTimers.Remove(departmentId);
        var data = await _dashboard.GetKpisAsync(new[] { departmentId }, null);
        await _hub.Clients.Group("kpi-admin")
            .SendAsync("AgentWorkloadUpdated", data.AgentWorkload);
        await _hub.Clients.Group("kpi-manager")
            .SendAsync("AgentWorkloadUpdated", data.AgentWorkload);
    }
}
