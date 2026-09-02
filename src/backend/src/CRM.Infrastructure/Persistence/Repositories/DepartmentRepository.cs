using CRM.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context) => _context = context;

    public async Task<Department?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default)
        => await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Departments.AnyAsync(d => d.Name == name, ct);

    public async Task AddAsync(Department dept, CancellationToken ct = default)
        => await _context.Departments.AddAsync(dept, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
