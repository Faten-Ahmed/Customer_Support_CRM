namespace CRM.Application.Reports.DTOs;

public record CsatReportDto(
    CsatOverallDto Overall,
    Dictionary<int, int> Distribution,
    IReadOnlyList<CsatByDepartmentDto> ByDepartment,
    IReadOnlyList<CsatByAgentDto> ByAgent,
    IReadOnlyList<string> RecentComments);

public record CsatOverallDto(
    decimal? AvgRating, int TotalSent, int TotalSubmitted, decimal ResponseRate);

public record CsatByDepartmentDto(
    Guid DepartmentId, string DepartmentName, decimal? AvgRating, int TotalSubmitted);

public record CsatByAgentDto(
    Guid AgentId, string AgentName, decimal? AvgRating, int TotalSubmitted);
