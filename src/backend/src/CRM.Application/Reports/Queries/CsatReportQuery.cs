using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record CsatReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<CsatReportDto>;

public class CsatReportQueryHandler
    : IRequestHandler<CsatReportQuery, CsatReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public CsatReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<CsatReportDto> Handle(CsatReportQuery query, CancellationToken ct)
    {
        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole is UserRole.Agent or UserRole.Manager)
        {
            var userDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!userDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Access to this department's report is not permitted.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = userDeptIds;
            }
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _reports.GetCsatReportAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, ct);

        return new CsatReportDto(
            new CsatOverallDto(
                data.Overall.AvgRating,
                data.Overall.TotalSent,
                data.Overall.TotalSubmitted,
                data.Overall.ResponseRate),
            data.Distribution,
            data.ByDepartment.Select(d => new CsatByDepartmentDto(
                d.DepartmentId, d.DepartmentName, d.AvgRating, d.TotalSubmitted)).ToList(),
            data.ByAgent.Select(a => new CsatByAgentDto(
                a.AgentId, a.AgentName, a.AvgRating, a.TotalSubmitted)).ToList(),
            data.RecentComments);
    }
}
