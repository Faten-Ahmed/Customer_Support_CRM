using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Chat.Commands.CloseSession;

public record CloseSessionCommand(Guid SessionId, string? Resolution = null) : IRequest;

public class CloseSessionCommandHandler : IRequestHandler<CloseSessionCommand>
{
    private readonly IChatSessionRepository _repo;
    private readonly ITicketRepository _tickets;

    public CloseSessionCommandHandler(IChatSessionRepository repo, ITicketRepository tickets)
    {
        _repo = repo;
        _tickets = tickets;
    }

    public async Task Handle(CloseSessionCommand req, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(req.SessionId, ct)
            ?? throw new InvalidOperationException($"Chat session {req.SessionId} not found.");

        if (session.Status == ChatSessionStatus.Closed) return;

        session.Close();
        await _repo.SaveAsync(ct);

        if (session.LinkedTicketId.HasValue && req.Resolution == "Resolved")
        {
            var ticket = await _tickets.FindByIdAsync(session.LinkedTicketId.Value, ct);
            if (ticket is not null)
            {
                ticket.ChangeStatus(TicketStatus.Resolved, session.AgentId ?? session.CustomerId);
                await _tickets.SaveChangesAsync(ct);
            }
        }
    }
}
