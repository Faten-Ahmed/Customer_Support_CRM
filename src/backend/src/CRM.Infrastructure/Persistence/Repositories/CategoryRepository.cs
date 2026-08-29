using CRM.Domain.Categories;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    public Task<TicketCategory?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<TicketCategory?>(null);

    public Task<IReadOnlyList<TicketCategory>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TicketCategory>>(new List<TicketCategory>());

    public Task<IReadOnlyList<TicketCategory>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TicketCategory>>(new List<TicketCategory>());

    public Task<bool> IsChildCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task AddAsync(TicketCategory category, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
