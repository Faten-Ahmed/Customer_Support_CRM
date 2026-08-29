using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? FirstNameAr = null,
    string? LastNameAr = null,
    string? JobTitle = null,
    string? JobTitleAr = null) : IRequest<UserProfileDto>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserProfileDto>
{
    private readonly IUserRepository _users;

    public UpdateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserProfileDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        user.UpdateProfile(cmd.FirstName, cmd.LastName,
            cmd.FirstNameAr, cmd.LastNameAr, cmd.JobTitle, cmd.JobTitleAr);
        await _users.SaveChangesAsync(ct);

        return CreateInternalUserCommandHandler.Map(user);
    }
}
