using CRM.Domain.Common;
using CRM.Domain.KnowledgeBase;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class KbArticleRepository : IKbArticleRepository
{
    private readonly AppDbContext _db;
    public KbArticleRepository(AppDbContext db) => _db = db;

    public Task<KbArticle?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.KbArticles.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<PagedResult<KbArticle>> ListAsync(
        KbArticleFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.KbArticles.AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(a => a.Status == filter.Status.Value);

        if (filter.CategoryId.HasValue)
            q = q.Where(a => a.CategoryId == filter.CategoryId.Value);

        if (filter.Visibility.HasValue)
        {
            // Portal callers: Public OR Both
            if (filter.Visibility.Value == KbVisibility.Public)
                q = q.Where(a => a.Visibility == KbVisibility.Public || a.Visibility == KbVisibility.Both);
            else
                q = q.Where(a => a.Visibility == filter.Visibility.Value);
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<KbArticle>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<KbArticle>> SearchAsync(
        string query, bool portalOnly, int limit, CancellationToken ct = default)
    {
        // SQL Server FTS via CONTAINS — falls back to LIKE for dev environments without FTS
        var q = _db.KbArticles.AsQueryable()
            .Where(a => a.Status == KbArticleStatus.Published);

        if (portalOnly)
            q = q.Where(a => a.Visibility == KbVisibility.Public || a.Visibility == KbVisibility.Both);

        // Simple LIKE search (FTS index is enabled on these columns in production)
        q = q.Where(a =>
            (a.Title != null && EF.Functions.Like(a.Title, $"%{query}%")) ||
            (a.TitleAr != null && EF.Functions.Like(a.TitleAr, $"%{query}%")) ||
            (a.Content != null && EF.Functions.Like(a.Content, $"%{query}%")) ||
            (a.ContentAr != null && EF.Functions.Like(a.ContentAr, $"%{query}%")));

        return await q
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(KbArticle article, CancellationToken ct = default)
        => await _db.KbArticles.AddAsync(article, ct);

    public Task RemoveAsync(KbArticle article, CancellationToken ct = default)
    {
        _db.KbArticles.Remove(article);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
