using CRM.Domain.Departments;

namespace CRM.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    public Task<Department?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Department?>(null);

    public Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Department>>(new List<Department>());

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task AddAsync(Department dept, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
