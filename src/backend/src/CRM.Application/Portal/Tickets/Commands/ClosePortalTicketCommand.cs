using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Portal.Tickets.Commands;

public record ClosePortalTicketCommand(Guid TicketId, Guid CustomerId)
    : IRequest<ClosePortalTicketResult>;

public record ClosePortalTicketResult(Guid Id, string Status, string? SurveyUrl = null);

public class ClosePortalTicketCommandHandler
    : IRequestHandler<ClosePortalTicketCommand, ClosePortalTicketResult>
{
    private readonly ITicketRepository _tickets;

    public ClosePortalTicketCommandHandler(ITicketRepository tickets)
        => _tickets = tickets;

    public async Task<ClosePortalTicketResult> Handle(
        ClosePortalTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.CustomerId != cmd.CustomerId)
            throw new UnauthorizedAccessException("You can only close your own tickets.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("TICKET_ALREADY_CLOSED");

        ticket.CloseByCustomer();
        await _tickets.SaveChangesAsync(ct);

        return new ClosePortalTicketResult(ticket.Id, ticket.Status.ToString());
    }
}
