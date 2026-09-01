namespace CRM.Application.Reports.DTOs;

public record TicketVolumeReportDto(
    VolumeSummary Summary,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByPriority,
    Dictionary<string, int> ByChannel,
    IReadOnlyList<TrendPointDto> Trend);

public record VolumeSummary(
    int TotalCreated, int TotalResolved, int TotalClosed, int OpenAtEndOfPeriod);

public record TrendPointDto(string Date, int Created, int Resolved);
