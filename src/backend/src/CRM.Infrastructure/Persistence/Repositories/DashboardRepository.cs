using CRM.Domain.Dashboard;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db) => _db = db;

    public async Task<DashboardKpiData> GetKpisAsync(
        IReadOnlyList<Guid>? departmentIds,
        Guid? agentId,
        CancellationToken ct = default)
    {
        var openStatuses = new[]
        {
            TicketStatus.New, TicketStatus.Assigned, TicketStatus.InProgress,
            TicketStatus.OnHold, TicketStatus.Escalated, TicketStatus.Reopened
        };

        var now   = DateTime.UtcNow;
        var today = now.Date;
        var last7  = now.AddDays(-7);
        var last30 = now.AddDays(-30);

        var openQuery = _db.Tickets.Where(t => openStatuses.Contains(t.Status));
        var allQuery  = _db.Tickets.AsQueryable();

        if (departmentIds?.Count > 0)
        {
            openQuery = openQuery.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));
            allQuery  = allQuery.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));
        }
        if (agentId.HasValue)
        {
            openQuery = openQuery.Where(t => t.AssignedToUserId == agentId);
            allQuery  = allQuery.Where(t => t.AssignedToUserId == agentId);
        }

        var openTickets = await openQuery.CountAsync(ct);

        var openByPriorityRaw = await openQuery
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        var openByPriority = openByPriorityRaw.ToDictionary(x => x.Priority, x => x.Count);

        var unassignedQuery = _db.Tickets
            .Where(t => t.Status == TicketStatus.New && t.AssignedToUserId == null);
        if (departmentIds?.Count > 0)
            unassignedQuery = unassignedQuery.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));
        var unassignedTickets = await unassignedQuery.CountAsync(ct);

        var ticketsTodayCreated  = await allQuery.CountAsync(t => t.CreatedAt >= today, ct);
        var ticketsTodayResolved = await allQuery.CountAsync(t => t.ResolvedAt.HasValue && t.ResolvedAt >= today, ct);

        // SLA breach rate — last 30 days
        var slaLast30 = await allQuery
            .Where(t => t.CreatedAt >= last30)
            .Join(_db.TicketSlas, t => t.Id, s => s.TicketId, (t, s) => new { s.ResolutionBreached })
            .ToListAsync(ct);

        var slaBreachRate = slaLast30.Count > 0
            ? Math.Round((decimal)slaLast30.Count(s => s.ResolutionBreached) / slaLast30.Count * 100, 1)
            : 0m;

        // Avg first response & resolution — last 7 days
        var sla7Day = await allQuery
            .Where(t => t.CreatedAt >= last7)
            .Join(_db.TicketSlas, t => t.Id, s => s.TicketId, (t, s) => new
            {
                t.CreatedAt,
                t.ResolvedAt,
                s.FirstResponseAt,
            })
            .ToListAsync(ct);

        var avgFr7Day = sla7Day
            .Where(s => s.FirstResponseAt.HasValue)
            .Select(s => (s.FirstResponseAt!.Value - s.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0).Average();

        var avgRes7Day = sla7Day
            .Where(s => s.ResolvedAt.HasValue)
            .Select(s => (s.ResolvedAt!.Value - s.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0).Average();

        // Escalation rate — last 30 days
        var statuses30 = await allQuery
            .Where(t => t.CreatedAt >= last30)
            .Select(t => t.Status)
            .ToListAsync(ct);

        var escalationRate = statuses30.Count > 0
            ? Math.Round((decimal)statuses30.Count(s => s == TicketStatus.Escalated) / statuses30.Count * 100, 1)
            : 0m;

        // Agent workload
        var agentUserQuery = _db.Users
            .Where(u => u.IsActive && u.Role == UserRole.Agent);
        if (departmentIds?.Count > 0)
            agentUserQuery = agentUserQuery.Where(u => u.Departments.Any(d => departmentIds.Contains(d.DepartmentId)));

        var agents = await agentUserQuery
            .Select(u => new
            {
                u.Id,
                FullName = u.FirstName + " " + u.LastName,
                u.AvailabilityStatus,
            })
            .ToListAsync(ct);

        var agentIds = agents.Select(a => a.Id).ToList();
        var agentOpenCounts = await _db.Tickets
            .Where(t => t.AssignedToUserId.HasValue
                        && agentIds.Contains(t.AssignedToUserId!.Value)
                        && openStatuses.Contains(t.Status))
            .GroupBy(t => t.AssignedToUserId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count, ct);

        var agentWorkload = agents
            .Select(a => new AgentWorkloadData(
                a.Id,
                a.FullName,
                agentOpenCounts.GetValueOrDefault(a.Id, 0),
                a.AvailabilityStatus.ToString()))
            .OrderByDescending(a => a.OpenTickets)
            .ToList();

        var utilizedAgents = agentWorkload.Count(a => a.OpenTickets > 0);
        var agentUtilization = agents.Count > 0
            ? Math.Round((decimal)utilizedAgents / agents.Count * 100, 1)
            : 0m;

        return new DashboardKpiData(
            openTickets,
            openByPriority,
            slaBreachRate,
            (decimal)Math.Round(avgFr7Day, 1),
            (decimal)Math.Round(avgRes7Day, 1),
            null,
            agentUtilization,
            ticketsTodayCreated,
            ticketsTodayResolved,
            escalationRate,
            unassignedTickets,
            agentWorkload,
            now);
    }
}
