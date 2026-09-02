using CRM.Domain.Chat;
using MediatR;

namespace CRM.Application.Chat.Queries;

public record GetWaitingSessionsQuery : IRequest<List<WaitingSessionDto>>;

public record WaitingSessionDto(Guid SessionId, string CustomerName, DateTime CreatedAt);

public class GetWaitingSessionsQueryHandler
    : IRequestHandler<GetWaitingSessionsQuery, List<WaitingSessionDto>>
{
    private readonly IChatSessionRepository _repo;

    public GetWaitingSessionsQueryHandler(IChatSessionRepository repo) => _repo = repo;

    public async Task<List<WaitingSessionDto>> Handle(
        GetWaitingSessionsQuery request, CancellationToken ct)
    {
        var sessions = await _repo.GetWaitingAsync(ct);
        return sessions
            .Select(s => new WaitingSessionDto(s.Id, s.CustomerName, s.CreatedAt))
            .ToList();
    }
}
