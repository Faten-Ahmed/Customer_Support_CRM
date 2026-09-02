namespace CRM.Domain.Branches;

public interface IBranchRepository
{
    Task<Branch?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Branch branch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
