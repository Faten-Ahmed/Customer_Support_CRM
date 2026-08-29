using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record AssignUserSkillsCommand(
    Guid UserId,
    IReadOnlyList<Guid> CategoryIds) : IRequest;

public class AssignUserSkillsCommandHandler : IRequestHandler<AssignUserSkillsCommand>
{
    private readonly IUserRepository _users;
    private readonly ICategoryExistenceChecker _categories;

    public AssignUserSkillsCommandHandler(
        IUserRepository users,
        ICategoryExistenceChecker categories)
    {
        _users = users;
        _categories = categories;
    }

    public async Task Handle(AssignUserSkillsCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        if (cmd.CategoryIds.Count > 0)
        {
            bool allExist = await _categories.AllExistAsync(cmd.CategoryIds, ct);
            if (!allExist)
                throw new InvalidOperationException(
                    "One or more category IDs do not exist.");
        }

        await _users.ReplaceUserSkillsAsync(user.Id, cmd.CategoryIds, ct);
        await _users.SaveChangesAsync(ct);
    }
}
