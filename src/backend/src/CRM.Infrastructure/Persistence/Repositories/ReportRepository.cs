using CRM.Domain.Reports;
using CRM.Domain.Sla;
using CRM.Domain.Surveys;
using CRM.Domain.Tickets.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;

    public ReportRepository(AppDbContext db) => _db = db;

    public async Task<TicketVolumeData> GetTicketVolumeAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        string groupBy,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Where(t => t.CreatedAt >= dateFrom && t.CreatedAt <= dateTo);

        if (departmentIds?.Count > 0)
            query = query.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));

        var tickets = await query
            .Select(t => new
            {
                t.Status,
                t.Priority,
                t.Channel,
                t.CreatedAt,
                t.ResolvedAt,
            })
            .ToListAsync(ct);

        var openStatuses = new HashSet<TicketStatus>
        {
            TicketStatus.New, TicketStatus.Assigned, TicketStatus.InProgress,
            TicketStatus.OnHold, TicketStatus.Escalated, TicketStatus.Reopened
        };

        var totalCreated = tickets.Count;
        var totalResolved = tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
        var totalClosed = tickets.Count(t => t.Status == TicketStatus.Closed);
        var openAtEnd = tickets.Count(t => openStatuses.Contains(t.Status));

        var byStatus = tickets
            .GroupBy(t => t.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byPriority = tickets
            .GroupBy(t => t.Priority.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byChannel = tickets
            .GroupBy(t => t.Channel.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Trend — bucket by day/week/month
        string BucketKey(DateTime d) => groupBy.ToLower() switch
        {
            "month" => d.ToString("yyyy-MM"),
            "week"  => $"{d:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(d):D2}",
            _       => d.ToString("yyyy-MM-dd"),
        };

        var createdBuckets = tickets
            .GroupBy(t => BucketKey(t.CreatedAt))
            .ToDictionary(g => g.Key, g => g.Count());

        var resolvedBuckets = tickets
            .Where(t => t.ResolvedAt.HasValue)
            .GroupBy(t => BucketKey(t.ResolvedAt!.Value))
            .ToDictionary(g => g.Key, g => g.Count());

        var allBuckets = createdBuckets.Keys
            .Union(resolvedBuckets.Keys)
            .OrderBy(k => k)
            .ToList();

        var trend = allBuckets
            .Select(b => new TrendPoint(
                b,
                createdBuckets.GetValueOrDefault(b, 0),
                resolvedBuckets.GetValueOrDefault(b, 0)))
            .ToList();

        return new TicketVolumeData(
            totalCreated, totalResolved, totalClosed, openAtEnd,
            byStatus, byPriority, byChannel, trend);
    }

    public async Task<SlaComplianceData> GetSlaComplianceAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        string? priority,
        CancellationToken ct = default)
    {
        var ticketQuery = _db.Tickets
            .Where(t => t.CreatedAt >= dateFrom && t.CreatedAt <= dateTo);

        if (departmentIds?.Count > 0)
            ticketQuery = ticketQuery.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));

        if (!string.IsNullOrWhiteSpace(priority) &&
            Enum.TryParse<TicketPriority>(priority, out var pEnum))
            ticketQuery = ticketQuery.Where(t => t.Priority == pEnum);

        var data = await ticketQuery
            .Join(_db.TicketSlas, t => t.Id, s => s.TicketId, (t, s) => new
            {
                t.Priority,
                t.CreatedAt,
                t.ResolvedAt,
                s.FirstResponseAt,
                s.FirstResponseBreached,
                s.ResolutionBreached,
                s.BreachTier,
                s.FirstResponseBreachTier,
            })
            .ToListAsync(ct);

        if (!data.Any())
            return new SlaComplianceData(0, 0, 0, 0, new Dictionary<string, SlaComplianceByPriority>(), new SlaBreachReasons(0, 0, 0));

        var total = data.Count;
        var frCompliant = data.Count(d => !d.FirstResponseBreached);
        var resCompliant = data.Count(d => !d.ResolutionBreached);

        var frRate = total > 0 ? Math.Round((decimal)frCompliant / total * 100, 1) : 0;
        var resRate = total > 0 ? Math.Round((decimal)resCompliant / total * 100, 1) : 0;

        var avgFrMinutes = data
            .Where(d => d.FirstResponseAt.HasValue)
            .Select(d => (d.FirstResponseAt!.Value - d.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var avgResMinutes = data
            .Where(d => d.ResolvedAt.HasValue)
            .Select(d => (d.ResolvedAt!.Value - d.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var byPriority = data
            .GroupBy(d => d.Priority.ToString())
            .ToDictionary(g => g.Key, g =>
            {
                var count = g.Count();
                var frC = g.Count(x => !x.FirstResponseBreached);
                var resC = g.Count(x => !x.ResolutionBreached);
                return new SlaComplianceByPriority(
                    count > 0 ? Math.Round((decimal)frC / count * 100, 1) : 0,
                    count > 0 ? Math.Round((decimal)resC / count * 100, 1) : 0,
                    count);
            });

        var breachReasons = new SlaBreachReasons(
            data.Count(d => d.BreachTier == SlaBreachTier.Warning || d.FirstResponseBreachTier == SlaBreachTier.Warning),
            data.Count(d => d.BreachTier == SlaBreachTier.Breach || d.FirstResponseBreachTier == SlaBreachTier.Breach),
            data.Count(d => d.BreachTier == SlaBreachTier.CriticalBreach || d.FirstResponseBreachTier == SlaBreachTier.CriticalBreach));

        return new SlaComplianceData(
            frRate, resRate,
            (decimal)Math.Round(avgFrMinutes, 1),
            (decimal)Math.Round(avgResMinutes, 1),
            byPriority, breachReasons);
    }

    public async Task<IReadOnlyList<AgentPerformanceData>> GetAgentPerformanceAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        CancellationToken ct = default)
    {
        var ticketQuery = _db.Tickets
            .Where(t => t.CreatedAt >= dateFrom && t.CreatedAt <= dateTo
                        && t.AssignedToUserId.HasValue);

        if (departmentIds?.Count > 0)
            ticketQuery = ticketQuery.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));

        var agentTickets = await ticketQuery
            .Join(_db.TicketSlas, t => t.Id, s => s.TicketId, (t, s) => new
            {
                t.AssignedToUserId,
                t.Status,
                t.CreatedAt,
                t.ResolvedAt,
                s.FirstResponseAt,
                s.FirstResponseBreached,
                s.ResolutionBreached,
                IsEscalated = t.Status == TicketStatus.Escalated,
            })
            .ToListAsync(ct);

        // Also fetch tickets without SLA records (left join equivalent)
        var allAgentTickets = await ticketQuery
            .Select(t => new
            {
                t.AssignedToUserId,
                t.Status,
                t.CreatedAt,
                t.ResolvedAt,
                IsEscalated = t.Status == TicketStatus.Escalated,
            })
            .ToListAsync(ct);

        var agentIds = allAgentTickets
            .Select(t => t.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var agentUsers = await _db.Users
            .Where(u => agentIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return agentIds.Select(agentId =>
        {
            var myTickets = allAgentTickets.Where(t => t.AssignedToUserId == agentId).ToList();
            var mySlaTickets = agentTickets.Where(t => t.AssignedToUserId == agentId).ToList();

            var handled = myTickets.Count;
            var resolved = myTickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
            var escalated = myTickets.Count(t => t.IsEscalated);

            var avgFr = mySlaTickets
                .Where(t => t.FirstResponseAt.HasValue)
                .Select(t => (t.FirstResponseAt!.Value - t.CreatedAt).TotalMinutes)
                .DefaultIfEmpty(0).Average();

            var avgRes = mySlaTickets
                .Where(t => t.ResolvedAt.HasValue)
                .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalMinutes)
                .DefaultIfEmpty(0).Average();

            var slaTotal = mySlaTickets.Count;
            var slaCompliant = mySlaTickets.Count(t => !t.ResolutionBreached);
            var slaRate = slaTotal > 0 ? Math.Round((decimal)slaCompliant / slaTotal * 100, 1) : 100m;

            var escalationRate = handled > 0 ? Math.Round((decimal)escalated / handled * 100, 1) : 0m;

            return new AgentPerformanceData(
                agentId,
                agentUsers.GetValueOrDefault(agentId, agentId.ToString()),
                handled, resolved,
                (decimal)Math.Round(avgFr, 1),
                (decimal)Math.Round(avgRes, 1),
                slaRate,
                null,   // CsatScore — Feature 12
                0,      // CsatResponseCount — Feature 12
                escalationRate);
        }).ToList();
    }

    public async Task<CsatReportData> GetCsatReportAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        CancellationToken ct = default)
    {
        var query = _db.CsatSurveys
            .Where(s => s.SentAt >= dateFrom && s.SentAt <= dateTo);

        if (departmentIds?.Count > 0)
            query = query.Where(s => departmentIds.Contains(s.DepartmentId));

        var surveys = await query.ToListAsync(ct);

        var totalSent = surveys.Count;
        var submitted = surveys.Where(s => s.Status == "Submitted").ToList();
        var totalSubmitted = submitted.Count;
        var responseRate = totalSent > 0
            ? Math.Round((decimal)totalSubmitted / totalSent * 100, 1)
            : 0m;
        var avgRating = submitted.Count > 0
            ? Math.Round((decimal)submitted.Average(s => s.Rating!.Value), 2)
            : (decimal?)null;

        var distribution = submitted
            .Where(s => s.Rating.HasValue)
            .GroupBy(s => s.Rating!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // By Department
        var deptIds = surveys.Select(s => s.DepartmentId).Distinct().ToList();
        var deptNames = await _db.Departments
            .Where(d => deptIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var byDepartment = surveys
            .GroupBy(s => s.DepartmentId)
            .Select(g =>
            {
                var sub = g.Where(s => s.Status == "Submitted").ToList();
                var avg = sub.Count > 0
                    ? Math.Round((decimal)sub.Average(s => s.Rating!.Value), 2)
                    : (decimal?)null;
                return new CsatByDepartmentData(
                    g.Key,
                    deptNames.GetValueOrDefault(g.Key, g.Key.ToString()),
                    avg,
                    sub.Count);
            })
            .ToList();

        // By Agent
        var agentIds = surveys.Select(s => s.AgentId).Where(id => id != Guid.Empty).Distinct().ToList();
        var agentNames = await _db.Users
            .Where(u => agentIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var byAgent = surveys
            .Where(s => s.AgentId != Guid.Empty)
            .GroupBy(s => s.AgentId)
            .Select(g =>
            {
                var sub = g.Where(s => s.Status == "Submitted").ToList();
                var avg = sub.Count > 0
                    ? Math.Round((decimal)sub.Average(s => s.Rating!.Value), 2)
                    : (decimal?)null;
                return new CsatByAgentData(
                    g.Key,
                    agentNames.GetValueOrDefault(g.Key, g.Key.ToString()),
                    avg,
                    sub.Count);
            })
            .ToList();

        var recentComments = submitted
            .Where(s => !string.IsNullOrWhiteSpace(s.Comment))
            .OrderByDescending(s => s.SubmittedAt)
            .Take(10)
            .Select(s => s.Comment!)
            .ToList();

        return new CsatReportData(
            new CsatOverallData(avgRating, totalSent, totalSubmitted, responseRate),
            distribution,
            byDepartment,
            byAgent,
            recentComments);
    }

    public async Task<int> CountRowsAsync(
        string reportType,
        IReadOnlyList<Guid>? departmentIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Where(t => t.CreatedAt >= dateFrom && t.CreatedAt <= dateTo);

        if (departmentIds?.Count > 0)
            query = query.Where(t => t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value));

        return await query.CountAsync(ct);
    }
}
