using CRM.Domain.Chat;
using MediatR;

namespace CRM.Application.Chat.Commands.AcceptHandoff;

public record AcceptHandoffCommand(Guid SessionId, Guid AgentId) : IRequest;

public class AcceptHandoffCommandHandler : IRequestHandler<AcceptHandoffCommand>
{
    private readonly IChatSessionRepository _repo;

    public AcceptHandoffCommandHandler(IChatSessionRepository repo) => _repo = repo;

    public async Task Handle(AcceptHandoffCommand req, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(req.SessionId, ct)
            ?? throw new InvalidOperationException($"Chat session {req.SessionId} not found.");

        if (session.Status != ChatSessionStatus.Waiting)
            throw new InvalidOperationException("Session is not in Waiting state.");

        session.AcceptHandoff(req.AgentId);
        await _repo.SaveAsync(ct);
    }
}
