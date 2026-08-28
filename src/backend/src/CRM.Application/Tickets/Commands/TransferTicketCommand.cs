using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record TransferTicketCommand(
    Guid TicketId,
    Guid? TargetDepartmentId,
    Guid? TargetAgentId,
    string Reason,
    Guid TransferredByUserId) : IRequest;

public class TransferTicketCommandHandler : IRequestHandler<TransferTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public TransferTicketCommandHandler(ITicketRepository tickets, IUserRepository users)
    {
        _tickets = tickets;
        _users = users;
    }

    public async Task Handle(TransferTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot transfer a ticket in {ticket.Status} status.");

        if (cmd.TargetAgentId.HasValue)
        {
            var agent = await _users.FindByIdAsync(cmd.TargetAgentId.Value, ct)
                ?? throw new KeyNotFoundException("Target agent not found.");
            if (!agent.IsActive)
                throw new InvalidOperationException("Cannot transfer to inactive agent.");
        }

        ticket.Transfer(cmd.TargetDepartmentId, cmd.TargetAgentId, cmd.Reason, cmd.TransferredByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
