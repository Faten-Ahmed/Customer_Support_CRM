namespace CRM.Application.Reports.Services;

public interface IReportExportJobScheduler
{
    string EnqueueExport(
        string reportType,
        string format,
        Guid[]? departmentIds,
        DateTime dateFrom,
        DateTime dateTo);
}
