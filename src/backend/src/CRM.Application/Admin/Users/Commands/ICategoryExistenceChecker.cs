namespace CRM.Application.Admin.Users.Commands;

public interface ICategoryExistenceChecker
{
    Task<bool> AllExistAsync(IEnumerable<Guid> categoryIds, CancellationToken ct = default);
}
