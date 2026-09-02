namespace CRM.Domain.Sla;

public class BusinessHours
{
    public Guid Id { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string[] WorkDays { get; private set; } = null!;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string TimeZone { get; private set; } = null!;

    private readonly List<Holiday> _holidays = new();
    public IReadOnlyList<Holiday> Holidays => _holidays.AsReadOnly();

    private BusinessHours() { }

    public static BusinessHours Create(
        string[] workDays, TimeOnly startTime, TimeOnly endTime,
        string timeZone, Guid? departmentId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WorkDays = workDays,
            StartTime = startTime,
            EndTime = endTime,
            TimeZone = timeZone,
            DepartmentId = departmentId
        };

    public void Update(string[] workDays, TimeOnly startTime, TimeOnly endTime, string timeZone)
    {
        WorkDays = workDays;
        StartTime = startTime;
        EndTime = endTime;
        TimeZone = timeZone;
    }

    public Holiday AddHoliday(DateOnly date, string name)
    {
        if (_holidays.Any(h => h.Date == date))
            throw new InvalidOperationException($"Holiday already exists on {date:yyyy-MM-dd}.");
        var holiday = Holiday.Create(Id, date, name);
        _holidays.Add(holiday);
        return holiday;
    }

    public void RemoveHoliday(Guid holidayId)
    {
        var holiday = _holidays.FirstOrDefault(h => h.Id == holidayId)
            ?? throw new KeyNotFoundException($"Holiday {holidayId} not found.");
        _holidays.Remove(holiday);
    }
}

public class Holiday
{
    public Guid Id { get; private set; }
    public Guid BusinessHoursId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = null!;

    private Holiday() { }

    public static Holiday Create(Guid businessHoursId, DateOnly date, string name)
        => new() { Id = Guid.NewGuid(), BusinessHoursId = businessHoursId, Date = date, Name = name };
}
