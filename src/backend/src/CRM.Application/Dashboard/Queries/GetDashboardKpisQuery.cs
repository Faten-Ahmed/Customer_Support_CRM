using CRM.Application.Dashboard.DTOs;
using CRM.Domain.Dashboard;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Dashboard.Queries;

public record GetDashboardKpisQuery(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<DashboardKpiDto>;

public class GetDashboardKpisQueryHandler
    : IRequestHandler<GetDashboardKpisQuery, DashboardKpiDto>
{
    private readonly IDashboardRepository _dashboard;
    private readonly IUserRepository _users;

    public GetDashboardKpisQueryHandler(IDashboardRepository dashboard, IUserRepository users)
    {
        _dashboard = dashboard;
        _users = users;
    }

    public async Task<DashboardKpiDto> Handle(
        GetDashboardKpisQuery query, CancellationToken ct)
    {
        IReadOnlyList<Guid>? effectiveDepartmentIds = null;
        Guid? effectiveAgentId = null;
        bool includeWorkload = true;

        if (query.RequestingUserRole == UserRole.Agent)
        {
            var agentDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            effectiveDepartmentIds = agentDeptIds;
            effectiveAgentId = query.RequestingUserId;
            includeWorkload = false;
        }
        else if (query.RequestingUserRole == UserRole.Manager)
        {
            var managerDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            effectiveDepartmentIds = query.DepartmentId.HasValue
                ? new[] { query.DepartmentId.Value }
                : managerDeptIds;
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _dashboard.GetKpisAsync(
            effectiveDepartmentIds, effectiveAgentId, ct);

        return new DashboardKpiDto(
            data.OpenTickets,
            data.OpenByPriority,
            data.SlaBreachRate,
            data.AvgFirstResponseMinutes7Day,
            data.AvgResolutionMinutes7Day,
            data.CsatScore30Day,
            data.AgentUtilization,
            data.TicketsTodayCreated,
            data.TicketsTodayResolved,
            data.EscalationRate,
            data.UnassignedTickets,
            includeWorkload
                ? data.AgentWorkload.Select(w => new AgentWorkloadDto(
                    w.AgentId, w.AgentName, w.OpenTickets, w.AvailabilityStatus)).ToList()
                : null,
            data.CalculatedAt);
    }
}
