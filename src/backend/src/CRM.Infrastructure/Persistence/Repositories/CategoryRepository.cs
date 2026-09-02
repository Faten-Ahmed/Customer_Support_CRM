using CRM.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    public async Task<TicketCategory?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TicketCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<TicketCategory>> ListAllAsync(CancellationToken ct = default)
        => await _context.TicketCategories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TicketCategory>> GetChildrenAsync(
        Guid parentId, CancellationToken ct = default)
        => await _context.TicketCategories
            .Where(c => c.ParentCategoryId == parentId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public async Task<bool> IsChildCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await _context.TicketCategories
            .AnyAsync(c => c.Id == categoryId && c.ParentCategoryId != null, ct);

    public async Task AddAsync(TicketCategory category, CancellationToken ct = default)
        => await _context.TicketCategories.AddAsync(category, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
