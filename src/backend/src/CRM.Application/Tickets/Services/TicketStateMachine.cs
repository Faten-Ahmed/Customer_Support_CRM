using CRM.Domain.Tickets.Enums;

namespace CRM.Application.Tickets.Services;

public static class TicketStateMachine
{
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> AllowedTransitions = new()
    {
        [TicketStatus.New]        = new() { TicketStatus.Assigned },
        [TicketStatus.Assigned]   = new() { TicketStatus.InProgress },
        [TicketStatus.InProgress] = new() { TicketStatus.OnHold, TicketStatus.Escalated, TicketStatus.Resolved },
        [TicketStatus.OnHold]     = new() { TicketStatus.InProgress },
        [TicketStatus.Escalated]  = new() { TicketStatus.InProgress, TicketStatus.Resolved },
        [TicketStatus.Resolved]   = new() { TicketStatus.Reopened, TicketStatus.Closed },
        [TicketStatus.Reopened]   = new() { TicketStatus.Assigned, TicketStatus.InProgress },
        [TicketStatus.Closed]     = new()
    };

    public static bool IsValidTransition(TicketStatus from, TicketStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
