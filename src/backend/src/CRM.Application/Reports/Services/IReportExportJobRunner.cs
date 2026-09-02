namespace CRM.Application.Reports.Services;

public interface IReportExportJobRunner
{
    Task ExecuteAsync(
        string reportType, string format,
        Guid[]? departmentIds, DateTime dateFrom, DateTime dateTo);
}
