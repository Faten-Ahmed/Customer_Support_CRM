namespace CRM.Domain.Sla;

public enum SlaBreachTier { None, Warning, Breach, CriticalBreach }

public class TicketSla
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid SlaPolicyId { get; private set; }
    public DateTime ClockStartedAt { get; private set; }
    public DateTime? ClockPausedAt { get; private set; }
    public int AccumulatedPauseMinutes { get; private set; }
    public DateTime? FirstResponseDue { get; private set; }
    public DateTime? ResolutionDue { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public bool FirstResponseBreached { get; private set; }
    public bool ResolutionBreached { get; private set; }
    public SlaBreachTier FirstResponseBreachTier { get; private set; }
    public SlaBreachTier BreachTier { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TicketSla() { }

    public static TicketSla Create(
        Guid ticketId, Guid slaPolicyId,
        DateTime clockStartedAt,
        DateTime? firstResponseDue,
        DateTime? resolutionDue)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SlaPolicyId = slaPolicyId,
            ClockStartedAt = clockStartedAt,
            FirstResponseDue = firstResponseDue,
            ResolutionDue = resolutionDue,
            BreachTier = SlaBreachTier.None,
            UpdatedAt = DateTime.UtcNow
        };

    public void PauseClock()
    {
        ClockPausedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResumeClock(int businessPauseMinutes)
    {
        if (ClockPausedAt.HasValue)
        {
            AccumulatedPauseMinutes += businessPauseMinutes;
            ClockPausedAt = null;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFirstResponseBreachTier(SlaBreachTier tier)
    {
        FirstResponseBreachTier = tier;
        if (tier >= SlaBreachTier.Breach) FirstResponseBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBreachTier(SlaBreachTier tier)
    {
        BreachTier = tier;
        if (tier >= SlaBreachTier.Breach) ResolutionBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFirstResponse()
    {
        FirstResponseAt = DateTime.UtcNow;
        if (FirstResponseDue.HasValue && FirstResponseAt > FirstResponseDue)
            FirstResponseBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecalculateDeadlines(
        Guid newSlaPolicyId,
        DateTime? newFirstResponseDue,
        DateTime? newResolutionDue)
    {
        SlaPolicyId = newSlaPolicyId;
        FirstResponseDue = newFirstResponseDue;
        ResolutionDue = newResolutionDue;
        BreachTier = SlaBreachTier.None;
        UpdatedAt = DateTime.UtcNow;
    }
}
