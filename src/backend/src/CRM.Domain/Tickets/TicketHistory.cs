namespace CRM.Domain.Tickets;

public class TicketHistory
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string FieldChanged { get; private set; } = null!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private TicketHistory() { }

    public static TicketHistory Create(Guid ticketId, string field,
        string? oldValue, string? newValue, Guid changedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            FieldChanged = field,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedBy,
            ChangedAt = DateTime.UtcNow
        };
}
