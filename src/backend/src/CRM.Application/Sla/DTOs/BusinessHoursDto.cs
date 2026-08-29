namespace CRM.Application.Sla.DTOs;

public record HolidayDto(Guid Id, string Date, string Name);

public record BusinessHoursDto(
    Guid Id,
    Guid? DepartmentId,
    string[] WorkDays,
    string StartTime,
    string EndTime,
    string TimeZone,
    IReadOnlyList<HolidayDto> Holidays);
