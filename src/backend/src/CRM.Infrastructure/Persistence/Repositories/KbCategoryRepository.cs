using CRM.Domain.KnowledgeBase;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class KbCategoryRepository : IKbCategoryRepository
{
    private readonly AppDbContext _db;
    public KbCategoryRepository(AppDbContext db) => _db = db;

    public Task<KbCategory?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.KbCategories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public async Task<IReadOnlyList<KbCategory>> ListActiveAsync(CancellationToken ct = default)
        => await _db.KbCategories.Where(c => c.IsActive).ToListAsync(ct);

    public async Task AddAsync(KbCategory category, CancellationToken ct = default)
        => await _db.KbCategories.AddAsync(category, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
