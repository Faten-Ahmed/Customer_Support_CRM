using CRM.Application.Reports.Services;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using Hangfire;
using MediatR;

namespace CRM.Application.Reports.Commands;

public record ExportReportCommand(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string ReportType,
    string Format,
    DateTime DateFrom,
    DateTime DateTo,
    Guid? DepartmentId) : IRequest<ExportReportResult>;

public record ExportReportResult(
    bool IsAsync,
    byte[]? FileBytes,
    string? ContentType,
    string? FileName,
    string? JobId);

public class ExportReportCommandHandler : IRequestHandler<ExportReportCommand, ExportReportResult>
{
    private static readonly string[] ValidReportTypes = ["tickets", "sla", "agents", "csat"];
    private static readonly string[] ValidFormats = ["csv", "xlsx", "pdf"];
    private const int AsyncThreshold = 10_000;

    private readonly IReportRepository _repo;
    private readonly IReportExportService _exporter;
    private readonly IBackgroundJobClient _jobs;
    private readonly IUserRepository _users;

    public ExportReportCommandHandler(
        IReportRepository repo,
        IReportExportService exporter,
        IBackgroundJobClient jobs,
        IUserRepository users)
    {
        _repo = repo;
        _exporter = exporter;
        _jobs = jobs;
        _users = users;
    }

    public async Task<ExportReportResult> Handle(
        ExportReportCommand cmd, CancellationToken ct)
    {
        if (cmd.RequestingUserRole == UserRole.Agent)
            throw new UnauthorizedAccessException(
                "Agents are not permitted to export reports.");

        if (!ValidReportTypes.Contains(cmd.ReportType))
            throw new ArgumentException($"Unknown report type: {cmd.ReportType}");

        if (!ValidFormats.Contains(cmd.Format))
            throw new ArgumentException($"Unknown format: {cmd.Format}");

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;
        if (cmd.RequestingUserRole == UserRole.Manager)
        {
            var depts = await _users.GetDepartmentIdsAsync(cmd.RequestingUserId, ct);
            effectiveDepartmentIds = cmd.DepartmentId.HasValue
                ? new[] { cmd.DepartmentId.Value }
                : depts;
        }
        else if (cmd.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { cmd.DepartmentId.Value };
        }

        var rowCount = await _repo.CountRowsAsync(
            cmd.ReportType, effectiveDepartmentIds, cmd.DateFrom, cmd.DateTo, ct);

        if (rowCount > AsyncThreshold)
        {
            var deptArray = effectiveDepartmentIds?.ToArray();
            var jobId = _jobs.Enqueue<IReportExportJobRunner>(
                j => j.ExecuteAsync(
                    cmd.ReportType, cmd.Format,
                    deptArray,
                    cmd.DateFrom, cmd.DateTo));
            return new ExportReportResult(true, null, null, null, jobId);
        }

        var bytes = await _exporter.ExportAsync(
            cmd.ReportType, cmd.Format, effectiveDepartmentIds,
            cmd.DateFrom, cmd.DateTo, ct);

        var (contentType, ext) = cmd.Format switch
        {
            "csv" => ("text/csv", "csv"),
            "xlsx" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "pdf" => ("application/pdf", "pdf"),
            _ => ("application/octet-stream", "bin")
        };

        var fileName = $"report-{cmd.ReportType}-{cmd.DateFrom:yyyy-MM-dd}.{ext}";
        return new ExportReportResult(false, bytes, contentType, fileName, null);
    }
}
