using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record ReactivateUserCommand(Guid TargetUserId) : IRequest<UserActiveResult>;

public class ReactivateUserCommandHandler
    : IRequestHandler<ReactivateUserCommand, UserActiveResult>
{
    private readonly IUserRepository _users;

    public ReactivateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserActiveResult> Handle(
        ReactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.TargetUserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.TargetUserId} not found.");

        user.Reactivate();
        await _users.SaveChangesAsync(ct);

        return new UserActiveResult(user.Id, user.IsActive);
    }
}
