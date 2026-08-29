namespace CRM.Domain.Channels;

public record LiveChatStats(int ActiveSessions, int PendingHandoffs);

public interface ILiveChatSessionRepository
{
    Task<LiveChatStats> GetStatsAsync(CancellationToken ct = default);
}
