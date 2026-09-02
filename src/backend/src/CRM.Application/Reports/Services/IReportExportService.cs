namespace CRM.Application.Reports.Services;

public interface IReportExportService
{
    Task<byte[]> ExportAsync(
        string reportType,
        string format,
        IReadOnlyList<Guid>? departmentIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default);
}
