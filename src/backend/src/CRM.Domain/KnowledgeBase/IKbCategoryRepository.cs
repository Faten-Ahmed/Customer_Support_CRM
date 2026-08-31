namespace CRM.Domain.KnowledgeBase;

public interface IKbCategoryRepository
{
    Task<KbCategory?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<KbCategory>> ListActiveAsync(CancellationToken ct = default);
    Task AddAsync(KbCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
