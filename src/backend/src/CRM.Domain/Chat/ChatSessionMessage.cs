namespace CRM.Domain.Chat;

public class ChatSessionMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string SenderRole { get; private set; } = string.Empty; // Customer | Agent | System
    public Guid? SenderId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }

    private ChatSessionMessage() { }

    public static ChatSessionMessage Create(
        Guid sessionId, string senderRole, Guid? senderId, string body) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        SenderRole = senderRole,
        SenderId = senderId,
        Body = body,
        SentAt = DateTime.UtcNow,
    };
}
