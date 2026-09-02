using CRM.Application.Admin.Users.Commands;

namespace CRM.Infrastructure.Identity;

public class CategoryExistenceChecker : ICategoryExistenceChecker
{
    public Task<bool> AllExistAsync(IEnumerable<Guid> categoryIds, CancellationToken ct = default)
        => Task.FromResult(true);
}
