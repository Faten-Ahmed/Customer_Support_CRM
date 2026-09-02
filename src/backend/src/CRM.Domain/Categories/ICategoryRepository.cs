namespace CRM.Domain.Categories;

public interface ICategoryRepository
{
    Task<TicketCategory?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketCategory>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketCategory>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);
    Task<bool> IsChildCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task AddAsync(TicketCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
