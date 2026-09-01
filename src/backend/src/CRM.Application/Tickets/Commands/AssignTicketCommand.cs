using CRM.Application.Notifications.Commands;
using CRM.Domain.Notifications;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record AssignTicketCommand(
    Guid TicketId,
    Guid AgentId,
    Guid AssignedByUserId) : IRequest;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IMediator _mediator;

    public AssignTicketCommandHandler(ITicketRepository tickets, IUserRepository users, IMediator mediator)
    {
        _tickets = tickets;
        _users = users;
        _mediator = mediator;
    }

    public async Task Handle(AssignTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot assign a ticket in {ticket.Status} status.");

        var agent = await _users.FindByIdAsync(cmd.AgentId, ct)
            ?? throw new KeyNotFoundException($"Agent {cmd.AgentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException("Cannot assign to an inactive agent.");

        ticket.Assign(cmd.AgentId, cmd.AssignedByUserId);
        await _tickets.SaveChangesAsync(ct);

        await _mediator.Send(new CreateNotificationCommand(
            cmd.AgentId,
            NotificationType.TicketAssigned,
            $"Ticket Assigned: #{ticket.TicketNumber}",
            $"You have been assigned ticket #{ticket.TicketNumber}: \"{ticket.Subject}\".",
            "ticket", ticket.Id), ct);
    }
}
