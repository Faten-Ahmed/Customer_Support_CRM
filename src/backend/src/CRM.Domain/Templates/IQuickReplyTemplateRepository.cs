using CRM.Domain.Common;

namespace CRM.Domain.Templates;

public interface IQuickReplyTemplateRepository
{
    Task<QuickReplyTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<QuickReplyTemplate>> ListForAgentAsync(
        Guid agentId, TemplateScope? scope, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(QuickReplyTemplate template, CancellationToken ct = default);
    Task RemoveAsync(QuickReplyTemplate template, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
