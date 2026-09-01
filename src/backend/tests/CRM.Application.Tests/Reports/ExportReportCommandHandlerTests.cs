using CRM.Application.Reports.Commands;
using CRM.Application.Reports.Services;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class ExportReportCommandHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IReportExportService> _exporter = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ExportReportCommandHandler _handler;

    public ExportReportCommandHandlerTests()
    {
        _handler = new ExportReportCommandHandler(
            _repo.Object, _exporter.Object, _jobs.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AgentRole_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ExportReportCommand(agentId, UserRole.Agent, "tickets", "csv",
                    new DateTime(2025, 10, 1), new DateTime(2025, 10, 31), null),
                default));
    }

    [Fact]
    public async Task Handle_SmallDataset_ReturnsSynchronousFile()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);
        var csvBytes = new byte[] { 0x41, 0x42, 0x43 };

        _repo.Setup(r => r.CountRowsAsync("tickets", null, from, to, default))
             .ReturnsAsync(500);
        _exporter.Setup(e => e.ExportAsync("tickets", "csv", null, from, to, default))
                 .ReturnsAsync(csvBytes);

        var result = await _handler.Handle(
            new ExportReportCommand(adminId, UserRole.Admin, "tickets", "csv", from, to, null),
            default);

        Assert.False(result.IsAsync);
        Assert.Equal(csvBytes, result.FileBytes);
        Assert.Equal("text/csv", result.ContentType);
    }

    [Fact]
    public async Task Handle_LargeDataset_ReturnsJobId()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 10, 31);
        var jobId = "hangfire-job-123";

        _repo.Setup(r => r.CountRowsAsync("tickets", null, from, to, default))
             .ReturnsAsync(15000);
        _jobs.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
             .Returns(jobId);

        var result = await _handler.Handle(
            new ExportReportCommand(adminId, UserRole.Admin, "tickets", "csv", from, to, null),
            default);

        Assert.True(result.IsAsync);
        Assert.Equal(jobId, result.JobId);
    }

    [Fact]
    public async Task Handle_InvalidReportType_ThrowsArgumentException()
    {
        var adminId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new ExportReportCommand(adminId, UserRole.Admin, "invalid", "csv",
                    new DateTime(2025, 10, 1), new DateTime(2025, 10, 31), null),
                default));
    }
}
