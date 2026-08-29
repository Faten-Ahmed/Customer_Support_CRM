using CRM.Domain.Common;
using CRM.Domain.Templates;

namespace CRM.Infrastructure.Persistence.Repositories;

public class QuickReplyTemplateRepository : IQuickReplyTemplateRepository
{
    public Task<QuickReplyTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<QuickReplyTemplate?>(null);

    public Task<PagedResult<QuickReplyTemplate>> ListForAgentAsync(
        Guid agentId, TemplateScope? scope, string? search,
        int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<QuickReplyTemplate>(
            new List<QuickReplyTemplate>(), 0, page, pageSize));

    public Task AddAsync(QuickReplyTemplate template, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RemoveAsync(QuickReplyTemplate template, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
