using CRM.Domain.Common;
using CRM.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class QuickReplyTemplateRepository : IQuickReplyTemplateRepository
{
    private readonly AppDbContext _context;

    public QuickReplyTemplateRepository(AppDbContext context) => _context = context;

    public async Task<QuickReplyTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.QuickReplyTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<QuickReplyTemplate>> ListForAgentAsync(
        Guid agentId, TemplateScope? scope, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.QuickReplyTemplates
            .Where(t => t.Scope == TemplateScope.Global || t.CreatedByUserId == agentId);

        if (scope.HasValue)
            query = query.Where(t => t.Scope == scope.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(s) ||
                t.Content.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<QuickReplyTemplate>(items, total, page, pageSize);
    }

    public async Task AddAsync(QuickReplyTemplate template, CancellationToken ct = default)
        => await _context.QuickReplyTemplates.AddAsync(template, ct);

    public Task RemoveAsync(QuickReplyTemplate template, CancellationToken ct = default)
    {
        _context.QuickReplyTemplates.Remove(template);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
