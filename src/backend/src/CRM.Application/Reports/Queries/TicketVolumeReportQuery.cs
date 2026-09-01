using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record TicketVolumeReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId,
    string GroupBy) : IRequest<TicketVolumeReportDto>;

public class TicketVolumeReportQueryHandler
    : IRequestHandler<TicketVolumeReportQuery, TicketVolumeReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public TicketVolumeReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<TicketVolumeReportDto> Handle(
        TicketVolumeReportQuery query, CancellationToken ct)
    {
        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole == UserRole.Agent)
        {
            var agentDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!agentDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Agents can only view reports for their own departments.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = agentDeptIds;
            }
        }
        else if (query.RequestingUserRole == UserRole.Manager)
        {
            var managerDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!managerDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Managers can only view reports for their own departments.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = managerDeptIds;
            }
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _reports.GetTicketVolumeAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, query.GroupBy, ct);

        return new TicketVolumeReportDto(
            new VolumeSummary(
                data.TotalCreated, data.TotalResolved,
                data.TotalClosed, data.OpenAtEndOfPeriod),
            data.ByStatus,
            data.ByPriority,
            data.ByChannel,
            data.Trend.Select(t => new TrendPointDto(t.Date, t.Created, t.Resolved)).ToList());
    }
}
