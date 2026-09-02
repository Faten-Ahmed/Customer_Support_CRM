using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record AgentPerformanceReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<IReadOnlyList<AgentPerformanceDto>>;

public class AgentPerformanceReportQueryHandler
    : IRequestHandler<AgentPerformanceReportQuery, IReadOnlyList<AgentPerformanceDto>>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public AgentPerformanceReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<IReadOnlyList<AgentPerformanceDto>> Handle(
        AgentPerformanceReportQuery query, CancellationToken ct)
    {
        if (query.RequestingUserRole == UserRole.Agent)
            throw new UnauthorizedAccessException(
                "Agents are not permitted to view the agent performance report.");

        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole == UserRole.Manager)
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

        var data = await _reports.GetAgentPerformanceAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, ct);

        return data.Select(d => new AgentPerformanceDto(
            d.AgentId, d.AgentName,
            d.TicketsHandled, d.TicketsResolved,
            d.AvgFirstResponseMinutes, d.AvgResolutionMinutes,
            d.SlaComplianceRate, d.CsatScore, d.CsatResponseCount,
            d.EscalationRate)).ToList();
    }
}
