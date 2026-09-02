using CRM.Application.Dashboard.Queries;
using CRM.Domain.Dashboard;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Dashboard;

public class GetDashboardKpisQueryHandlerTests
{
    private readonly Mock<IDashboardRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetDashboardKpisQueryHandler _handler;

    public GetDashboardKpisQueryHandlerTests()
    {
        _handler = new GetDashboardKpisQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsOrgWideKpis()
    {
        var adminId = Guid.NewGuid();

        _repo.Setup(r => r.GetKpisAsync(null, null, default))
             .ReturnsAsync(new DashboardKpiData(
                 120, new Dictionary<string, int> { ["Critical"] = 5 },
                 12.5m, 8.0m, 240.0m, 4.3m, 85.0m, 30, 28, 5.2m, 15,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        var result = await _handler.Handle(
            new GetDashboardKpisQuery(adminId, UserRole.Admin, null),
            default);

        Assert.Equal(120, result.OpenTickets);
        Assert.NotNull(result.AgentWorkload);
    }

    [Fact]
    public async Task Handle_AgentRole_ReturnsPersonalKpisWithNoWorkloadArray()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetKpisAsync(It.IsAny<IReadOnlyList<Guid>?>(), agentId, default))
             .ReturnsAsync(new DashboardKpiData(
                 10, new Dictionary<string, int>(),
                 15.0m, 5.0m, 300.0m, 4.0m, 90.0m, 3, 2, 0.0m, 1,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        var result = await _handler.Handle(
            new GetDashboardKpisQuery(agentId, UserRole.Agent, null),
            default);

        Assert.Null(result.AgentWorkload);
    }

    [Fact]
    public async Task Handle_ManagerNoFilter_ScopesToOwnDepartments()
    {
        var managerId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _users.Setup(u => u.GetDepartmentIdsAsync(managerId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetKpisAsync(
            It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)),
            null, default))
             .ReturnsAsync(new DashboardKpiData(
                 40, new Dictionary<string, int>(),
                 11.0m, 6.0m, 200.0m, 4.1m, 88.0m, 10, 8, 3.0m, 5,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        await _handler.Handle(
            new GetDashboardKpisQuery(managerId, UserRole.Manager, null),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(managerId, default), Times.Once);
    }
}
