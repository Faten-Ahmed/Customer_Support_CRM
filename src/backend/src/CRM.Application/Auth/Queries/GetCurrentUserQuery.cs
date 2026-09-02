using CRM.Application.Auth.DTOs;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IUserRepository _users;

    public GetCurrentUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        return new CurrentUserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            FirstNameAr: user.FirstNameAr,
            LastName: user.LastName,
            LastNameAr: user.LastNameAr,
            JobTitle: user.JobTitle,
            JobTitleAr: user.JobTitleAr,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            RequiresPasswordChange: user.RequiresPasswordChange,
            AvatarUrl: null,
            DepartmentName: null);
    }
}
