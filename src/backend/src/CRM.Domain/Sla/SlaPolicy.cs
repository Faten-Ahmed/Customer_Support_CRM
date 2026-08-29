using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Sla;

public class SlaPolicy
{
    public Guid Id { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public TicketPriority Priority { get; private set; }
    public int FirstResponseMinutes { get; private set; }
    public int ResolutionMinutes { get; private set; }
    public int WarningThresholdPercent { get; private set; }
    public int BreachThresholdPercent { get; private set; }
    public int CriticalBreachThresholdPercent { get; private set; }

    private SlaPolicy() { }

    public static SlaPolicy Create(
        TicketPriority priority,
        int firstResponseMinutes,
        int resolutionMinutes,
        Guid? departmentId = null,
        int warningThresholdPercent = 80,
        int breachThresholdPercent = 100,
        int criticalBreachThresholdPercent = 200)
        => new()
        {
            Id = Guid.NewGuid(),
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            DepartmentId = departmentId,
            WarningThresholdPercent = warningThresholdPercent,
            BreachThresholdPercent = breachThresholdPercent,
            CriticalBreachThresholdPercent = criticalBreachThresholdPercent
        };

    public void Update(
        int firstResponseMinutes, int resolutionMinutes,
        int warningPercent, int breachPercent, int criticalPercent)
    {
        FirstResponseMinutes = firstResponseMinutes;
        ResolutionMinutes = resolutionMinutes;
        WarningThresholdPercent = warningPercent;
        BreachThresholdPercent = breachPercent;
        CriticalBreachThresholdPercent = criticalPercent;
    }
}
