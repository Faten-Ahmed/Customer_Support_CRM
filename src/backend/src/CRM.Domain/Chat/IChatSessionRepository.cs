namespace CRM.Domain.Chat;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ChatSession session, CancellationToken ct = default);
    Task AddMessageAsync(ChatSessionMessage message, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
