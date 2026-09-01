using CRM.Application.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
// AgentCapacityDto is in CRM.Domain.Users

namespace CRM.Application.Tickets.Jobs;

public class AutoAssignTicketJob
{
    private const int MaxOpenTickets = 15;

    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public AutoAssignTicketJob(
        ITicketRepository tickets,
        IUserRepository users,
        INotificationService notifications)
    {
        _tickets = tickets;
        _users = users;
        _notifications = notifications;
    }

    public async Task Execute(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var deptId = ticket.DepartmentId ?? Guid.Empty;
        var agents = await _users.FindActiveAgentsInDepartmentAsync(deptId, ct);

        if (!agents.Any())
        {
            await _notifications.SendUnassignedTicketAlertAsync(deptId, ticketId, ct);
            return;
        }

        // Step 1: skill match — pick skill-matched agent with fewest open tickets
        var skillMatched = agents
            .Where(a => ticket.CategoryId.HasValue
                        && a.SkillCategoryIds.Contains(ticket.CategoryId.Value)
                        && a.OpenTicketCount < MaxOpenTickets)
            .OrderBy(a => a.OpenTicketCount)
            .FirstOrDefault();

        // Step 2: round-robin fallback — oldest LastAssignedAt
        var roundRobin = agents
            .Where(a => a.OpenTicketCount < MaxOpenTickets)
            .OrderBy(a => a.LastAssignedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        var selected = skillMatched ?? roundRobin;

        if (selected is null)
        {
            await _notifications.SendUnassignedTicketAlertAsync(deptId, ticketId, ct);
            return;
        }

        ticket.Assign(selected.AgentId, Guid.Empty);
        await _users.UpdateLastAssignedAtAsync(selected.AgentId, ct);
        await _tickets.SaveChangesAsync(ct);
    }
}
