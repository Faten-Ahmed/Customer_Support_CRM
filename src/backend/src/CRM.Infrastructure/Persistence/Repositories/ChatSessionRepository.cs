using CRM.Domain.Chat;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly AppDbContext _db;

    public ChatSessionRepository(AppDbContext db) => _db = db;

    public async Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(ChatSession session, CancellationToken ct = default) =>
        await _db.ChatSessions.AddAsync(session, ct);

    public async Task AddMessageAsync(ChatSessionMessage message, CancellationToken ct = default) =>
        await _db.ChatSessionMessages.AddAsync(message, ct);

    public Task SaveAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
