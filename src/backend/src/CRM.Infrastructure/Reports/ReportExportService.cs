using CRM.Application.Reports.Services;

namespace CRM.Infrastructure.Reports;

/// <summary>
/// Stub implementation of IReportExportService.
/// Full CSV/XLSX/PDF generation will be implemented in a later iteration.
/// </summary>
public class ReportExportService : IReportExportService
{
    public Task<byte[]> ExportAsync(
        string reportType,
        string format,
        IReadOnlyList<Guid>? departmentIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default)
        => Task.FromResult(Array.Empty<byte>());
}
