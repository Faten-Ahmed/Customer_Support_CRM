using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class TicketFieldDefinitionRepository : ITicketFieldDefinitionRepository
{
    private readonly AppDbContext _context;

    public TicketFieldDefinitionRepository(AppDbContext context) => _context = context;

    public async Task<TicketFieldDefinition?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TicketFieldDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<TicketFieldDefinition>> GetActiveAsync(
        Guid? departmentId, Guid? categoryId, CancellationToken ct = default)
    {
        var query = _context.TicketFieldDefinitions.Where(f => f.IsActive);

        if (departmentId.HasValue)
            query = query.Where(f => f.DepartmentId == departmentId.Value);

        if (categoryId.HasValue)
            query = query.Where(f => f.CategoryId == categoryId.Value);

        return await query.OrderBy(f => f.SortOrder).ToListAsync(ct);
    }

    public async Task AddAsync(TicketFieldDefinition field, CancellationToken ct = default)
        => await _context.TicketFieldDefinitions.AddAsync(field, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
