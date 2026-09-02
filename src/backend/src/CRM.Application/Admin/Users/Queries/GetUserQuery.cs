using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Queries;

public record GetUserQuery(Guid UserId) : IRequest<UserDetailDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDetailDto>
{
    private readonly IUserRepository _users;

    public GetUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserDetailDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        var projection = await _users.GetDetailAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        var departments = projection.Departments
            .Select(d => new DepartmentAssignmentDto(d.DepartmentId, d.DepartmentName, d.IsPrimary))
            .ToList();

        var skills = projection.Skills
            .Select(s => new SkillDto(s.CategoryId, s.CategoryName))
            .ToList();

        return new UserDetailDto(
            projection.Id, projection.FirstName, projection.LastName,
            projection.FirstNameAr, projection.LastNameAr,
            projection.JobTitle, projection.JobTitleAr,
            projection.Email, projection.Role,
            projection.IsActive, projection.PasswordMustChange, projection.AvailabilityStatus,
            projection.CreatedAt.DateTime, departments, skills);
    }
}
