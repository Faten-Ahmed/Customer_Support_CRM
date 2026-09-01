using CRM.Application.Reports.Services;

namespace CRM.Infrastructure.Reports.Jobs;

public class GenerateReportExportJob : IReportExportJobRunner
{
    private readonly IReportExportService _exporter;

    public GenerateReportExportJob(IReportExportService exporter) => _exporter = exporter;

    public async Task ExecuteAsync(
        string reportType, string format,
        Guid[]? departmentIds, DateTime dateFrom, DateTime dateTo)
    {
        var bytes = await _exporter.ExportAsync(
            reportType, format, departmentIds, dateFrom, dateTo);
        // Store result (e.g., S3/blob) and notify requester; stub for now.
        _ = bytes;
    }
}
