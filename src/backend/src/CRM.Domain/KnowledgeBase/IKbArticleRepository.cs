using CRM.Domain.Common;

namespace CRM.Domain.KnowledgeBase;

public record KbArticleFilter(
    KbArticleStatus? Status,
    Guid? CategoryId,
    KbVisibility? Visibility);

public interface IKbArticleRepository
{
    Task<KbArticle?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<KbArticle>> ListAsync(
        KbArticleFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<KbArticle>> SearchAsync(
        string query, bool portalOnly, int limit, CancellationToken ct = default);
    Task AddAsync(KbArticle article, CancellationToken ct = default);
    Task RemoveAsync(KbArticle article, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
