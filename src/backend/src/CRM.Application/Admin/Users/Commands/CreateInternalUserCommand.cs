using CRM.Application.Admin.Users.DTOs;
using CRM.Application.Common;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record CreateInternalUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role,
    Guid? PrimaryDepartmentId,
    string? FirstNameAr = null,
    string? LastNameAr = null,
    string? JobTitle = null,
    string? JobTitleAr = null) : IRequest<UserProfileDto>;

public class CreateInternalUserCommandHandler
    : IRequestHandler<CreateInternalUserCommand, UserProfileDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IBackgroundJobService _jobs;

    public CreateInternalUserCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IBackgroundJobService jobs)
    {
        _users = users;
        _hasher = hasher;
        _jobs = jobs;
    }

    public async Task<UserProfileDto> Handle(
        CreateInternalUserCommand cmd, CancellationToken ct)
    {
        if (cmd.Role is UserRole.Agent or UserRole.Manager && cmd.PrimaryDepartmentId is null)
            throw new InvalidOperationException(
                "primaryDepartmentId is required for Agent and Manager roles.");

        bool emailExists = await _users.ExistsWithEmailAsync(cmd.Email, ct);
        if (emailExists)
            throw new InvalidOperationException(
                "409: A user with this email already exists.");

        var user = User.CreateInternal(
            Guid.NewGuid(),
            cmd.FirstName,
            cmd.LastName,
            cmd.Email,
            cmd.Role,
            cmd.FirstNameAr,
            cmd.LastNameAr,
            cmd.JobTitle,
            cmd.JobTitleAr);

        user.SetPassword(_hasher.Hash(cmd.Password), mustChange: true);

        if (cmd.PrimaryDepartmentId.HasValue)
        {
            user.ReplaceDepartments(new[]
            {
                new UserDepartment
                {
                    UserId = user.Id,
                    DepartmentId = cmd.PrimaryDepartmentId.Value,
                    IsPrimary = true
                }
            });
        }

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        _jobs.EnqueueWelcomeEmail(user.Id, cmd.Email, cmd.Password);

        return Map(user);
    }

    internal static UserProfileDto Map(User u)
        => new(u.Id, u.FirstName, u.LastName, u.FirstNameAr, u.LastNameAr,
               u.JobTitle, u.JobTitleAr, u.Email, u.Role.ToString(), u.IsActive,
               u.RequiresPasswordChange, u.AvailabilityStatus.ToString(), u.CreatedAt);
}
