using CRM.Domain.Branches;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly AppDbContext _context;

    public BranchRepository(AppDbContext context) => _context = context;

    public async Task<Branch?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default)
        => await _context.Branches.OrderBy(b => b.Name).ToListAsync(ct);

    public async Task AddAsync(Branch branch, CancellationToken ct = default)
        => await _context.Branches.AddAsync(branch, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
