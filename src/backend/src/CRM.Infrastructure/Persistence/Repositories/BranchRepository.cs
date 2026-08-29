using CRM.Domain.Branches;

namespace CRM.Infrastructure.Persistence.Repositories;

public class BranchRepository : IBranchRepository
{
    public Task<Branch?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Branch?>(null);

    public Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Branch>>(new List<Branch>());

    public Task AddAsync(Branch branch, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
