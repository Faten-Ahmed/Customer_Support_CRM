namespace CRM.Domain.Departments;

public interface IDepartmentRepository
{
    Task<Department?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Department dept, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
