using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class AgentPerformanceReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly AgentPerformanceReportQueryHandler _handler;

    public AgentPerformanceReportQueryHandlerTests()
    {
        _handler = new AgentPerformanceReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsAgentList()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);
        var agentId = Guid.NewGuid();

        _repo.Setup(r => r.GetAgentPerformanceAsync(from, to, null, default))
             .ReturnsAsync(new List<AgentPerformanceData>
             {
                 new(agentId, "Alice Smith", 45, 40, 15.3m, 240.1m, 93.5m, 4.2m, 2, 5.1m)
             });

        var result = await _handler.Handle(
            new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Single(result);
        Assert.Equal("Alice Smith", result[0].AgentName);
        Assert.Equal(45, result[0].TicketsHandled);
    }

    [Fact]
    public async Task Handle_AgentRole_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, agentId, UserRole.Agent, null),
                default));
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
                default));
    }

    [Fact]
    public async Task Handle_NoCsatResponses_CsatScoreIsNull()
    {
        var adminId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetAgentPerformanceAsync(from, to, null, default))
             .ReturnsAsync(new List<AgentPerformanceData>
             {
                 new(agentId, "Bob Jones", 10, 8, 20.0m, 300.0m, 80.0m, null, 0, 2.0m)
             });

        var result = await _handler.Handle(
            new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Null(result[0].CsatScore);
        Assert.Equal(0, result[0].CsatResponseCount);
    }

    [Fact]
    public async Task Handle_ManagerCrossDepartmentFilter_ThrowsUnauthorizedAccessException()
    {
        var managerId = Guid.NewGuid();
        var managerDeptId = Guid.NewGuid();
        var otherDeptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(managerId, default))
              .ReturnsAsync(new List<Guid> { managerDeptId });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, managerId, UserRole.Manager, otherDeptId),
                default));
    }
}
