using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class TicketVolumeReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly TicketVolumeReportQueryHandler _handler;

    public TicketVolumeReportQueryHandlerTests()
    {
        _handler = new TicketVolumeReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetTicketVolumeAsync(
            from, to, null, "day", default))
             .ReturnsAsync(new TicketVolumeData(
                 320, 298, 275, 22,
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new List<TrendPoint>()));

        var result = await _handler.Handle(
            new TicketVolumeReportQuery(from, to, adminId, UserRole.Admin, null, "day"),
            default);

        Assert.Equal(320, result.Summary.TotalCreated);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new TicketVolumeReportQuery(from, to, adminId, UserRole.Admin, null, "day"),
                default));
    }

    [Fact]
    public async Task Handle_AgentRequestingData_ScopesToOwnDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetTicketVolumeAsync(
            from, to, It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)), "day", default))
             .ReturnsAsync(new TicketVolumeData(
                 10, 9, 8, 2,
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new List<TrendPoint>()));

        var result = await _handler.Handle(
            new TicketVolumeReportQuery(from, to, agentId, UserRole.Agent, null, "day"),
            default);

        Assert.Equal(10, result.Summary.TotalCreated);
        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentRequestsOutOfScopeDept_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();
        var agentDeptId = Guid.NewGuid();
        var otherDeptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { agentDeptId });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new TicketVolumeReportQuery(from, to, agentId, UserRole.Agent, otherDeptId, "day"),
                default));
    }
}
