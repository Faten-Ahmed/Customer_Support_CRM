using CRM.Application.Common;
using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record ReopenTicketCommand(Guid TicketId, Guid ReopenedByUserId) : IRequest;

public class ReopenTicketCommandHandler : IRequestHandler<ReopenTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly ITicketJobScheduler _jobs;

    public ReopenTicketCommandHandler(
        ITicketRepository tickets,
        IUserRepository users,
        ITicketJobScheduler jobs)
    {
        _tickets = tickets;
        _users = users;
        _jobs = jobs;
    }

    public async Task Handle(ReopenTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot reopen a closed ticket.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, TicketStatus.Reopened))
            throw new InvalidOperationException(
                $"Cannot reopen a ticket in {ticket.Status} status.");

        ticket.ChangeStatus(TicketStatus.Reopened, cmd.ReopenedByUserId);

        if (ticket.AssignedToUserId.HasValue &&
            !await _users.IsActiveAgentAsync(ticket.AssignedToUserId.Value, ct))
        {
            _jobs.ScheduleAutoAssign(ticket.Id);
        }

        await _tickets.SaveChangesAsync(ct);
    }
}
