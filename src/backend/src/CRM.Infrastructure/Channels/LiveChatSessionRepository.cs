using CRM.Domain.Channels;

namespace CRM.Infrastructure.Channels;

public class LiveChatSessionRepository : ILiveChatSessionRepository
{
    public Task<LiveChatStats> GetStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new LiveChatStats(0, 0));
}
