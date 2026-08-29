using CRM.Application.Sla.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record TransferTicketCommand(
    Guid TicketId,
    Guid DepartmentId,
    string TransferNote,
    Guid TransferredByUserId) : IRequest;

public class TransferTicketCommandHandler : IRequestHandler<TransferTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IMediator _mediator;

    public TransferTicketCommandHandler(ITicketRepository tickets, IMediator mediator)
    {
        _tickets = tickets;
        _mediator = mediator;
    }

    public async Task Handle(TransferTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot transfer a ticket in {ticket.Status} status.");

        var departmentActive = await _tickets.IsDepartmentActiveAsync(cmd.DepartmentId, ct);
        if (!departmentActive)
            throw new InvalidOperationException("Target department not found or is inactive.");

        ticket.Transfer(cmd.DepartmentId, cmd.TransferNote, cmd.TransferredByUserId);
        await _tickets.SaveChangesAsync(ct);

        await _mediator.Send(
            new RecalculateSlaOnTransferCommand(cmd.TicketId, cmd.DepartmentId), ct);
    }
}
