namespace CRM.Domain.Tickets;

public class TicketMessage
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string Body { get; private set; } = null!;
    public bool IsInternal { get; private set; }
    public Guid? AuthorUserId { get; private set; }
    public Guid? AuthorCustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TicketMessage() { }

    public static TicketMessage Create(
        Guid ticketId,
        string body,
        bool isInternal,
        Guid? authorUserId,
        Guid? authorCustomerId)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message body is required.", nameof(body));

        return new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Body = body,
            IsInternal = isInternal,
            AuthorUserId = authorUserId,
            AuthorCustomerId = authorCustomerId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
