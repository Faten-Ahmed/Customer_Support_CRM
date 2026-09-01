using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record SlaComplianceReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId,
    string? Priority) : IRequest<SlaComplianceReportDto>;

public class SlaComplianceReportQueryHandler
    : IRequestHandler<SlaComplianceReportQuery, SlaComplianceReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public SlaComplianceReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<SlaComplianceReportDto> Handle(
        SlaComplianceReportQuery query, CancellationToken ct)
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

        var data = await _reports.GetSlaComplianceAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, query.Priority, ct);

        return new SlaComplianceReportDto(
            data.FirstResponseComplianceRate,
            data.ResolutionComplianceRate,
            data.AvgFirstResponseMinutes,
            data.AvgResolutionMinutes,
            data.ByPriority.ToDictionary(
                kvp => kvp.Key,
                kvp => new SlaComplianceByPriorityDto(
                    kvp.Value.FirstResponseComplianceRate,
                    kvp.Value.ResolutionComplianceRate,
                    kvp.Value.TotalTickets)),
            new SlaBreachReasonsDto(
                data.BreachReasons.WarningCount,
                data.BreachReasons.BreachCount,
                data.BreachReasons.CriticalBreachCount));
    }
}
