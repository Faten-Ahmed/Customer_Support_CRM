namespace CRM.Application.Sla.DTOs;

public record SlaPolicyDto(
    Guid Id,
    Guid? DepartmentId,
    string Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);
