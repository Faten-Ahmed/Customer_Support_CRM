using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record DeactivateUserCommand(Guid TargetUserId, Guid RequestingUserId)
    : IRequest<UserActiveResult>;

public record UserActiveResult(Guid Id, bool IsActive);

public class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, UserActiveResult>
{
    private readonly IUserRepository _users;

    public DeactivateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserActiveResult> Handle(
        DeactivateUserCommand cmd, CancellationToken ct)
    {
        if (cmd.TargetUserId == cmd.RequestingUserId)
            throw new InvalidOperationException(
                "CANNOT_DEACTIVATE_SELF: An admin cannot deactivate their own account.");

        var user = await _users.FindByIdAsync(cmd.TargetUserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.TargetUserId} not found.");

        if (user.Role == UserRole.Admin)
        {
            int activeAdmins = await _users.CountActiveAdminsAsync(ct);
            if (activeAdmins <= 1)
                throw new InvalidOperationException(
                    "CANNOT_DEACTIVATE_LAST_ADMIN: At least one active Admin must remain.");
        }

        user.Deactivate();
        await _users.SaveChangesAsync(ct);

        return new UserActiveResult(user.Id, user.IsActive);
    }
}
