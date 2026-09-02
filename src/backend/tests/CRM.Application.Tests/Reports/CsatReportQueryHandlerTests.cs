using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class CsatReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly CsatReportQueryHandler _handler;

    public CsatReportQueryHandlerTests()
    {
        _handler = new CsatReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsCsatReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetCsatReportAsync(from, to, null, default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(4.2m, 150, 120, 80.0m),
                 new Dictionary<int, int> { [1] = 2, [2] = 5, [3] = 10, [4] = 40, [5] = 63 },
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        var result = await _handler.Handle(
            new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Equal(4.2m, result.Overall.AvgRating);
        Assert.Equal(150, result.Overall.TotalSent);
        Assert.Equal(80.0m, result.Overall.ResponseRate);
    }

    [Fact]
    public async Task Handle_NoSubmissions_AvgRatingIsNull()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetCsatReportAsync(from, to, null, default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(null, 50, 0, 0.0m),
                 new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 },
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        var result = await _handler.Handle(
            new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Null(result.Overall.AvgRating);
        Assert.Equal(50, result.Overall.TotalSent);
        Assert.Equal(0, result.Overall.TotalSubmitted);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
                default));
    }

    [Fact]
    public async Task Handle_AgentScope_ScopesToOwnDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetCsatReportAsync(
            from, to, It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)), default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(4.0m, 10, 8, 80.0m),
                 new Dictionary<int, int>(),
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        await _handler.Handle(
            new CsatReportQuery(from, to, agentId, UserRole.Agent, null),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }
}
