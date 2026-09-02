using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class SlaComplianceReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly SlaComplianceReportQueryHandler _handler;

    public SlaComplianceReportQueryHandlerTests()
    {
        _handler = new SlaComplianceReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminAllDepts_ReturnsComplianceReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetSlaComplianceAsync(from, to, null, null, default))
             .ReturnsAsync(new SlaComplianceData(
                 92.5m, 88.3m, 14.2m, 240.5m,
                 new Dictionary<string, SlaComplianceByPriority>(),
                 new SlaBreachReasons(5, 12, 3)));

        var result = await _handler.Handle(
            new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, null),
            default);

        Assert.Equal(92.5m, result.FirstResponseComplianceRate);
        Assert.Equal(88.3m, result.ResolutionComplianceRate);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, null),
                default));
    }

    [Fact]
    public async Task Handle_AgentAccessesOtherDept_ThrowsUnauthorizedAccessException()
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
                new SlaComplianceReportQuery(from, to, agentId, UserRole.Agent, otherDeptId, null),
                default));
    }

    [Fact]
    public async Task Handle_PriorityFilter_PassedToRepository()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetSlaComplianceAsync(from, to, null, "Critical", default))
             .ReturnsAsync(new SlaComplianceData(
                 80.0m, 75.0m, 8.5m, 180.0m,
                 new Dictionary<string, SlaComplianceByPriority>(),
                 new SlaBreachReasons(2, 5, 1)));

        var result = await _handler.Handle(
            new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, "Critical"),
            default);

        _repo.Verify(r => r.GetSlaComplianceAsync(from, to, null, "Critical", default), Times.Once);
    }
}
