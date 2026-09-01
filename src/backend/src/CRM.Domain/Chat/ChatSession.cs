namespace CRM.Domain.Chat;

public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public Guid? AgentId { get; private set; }
    public Guid? LinkedTicketId { get; private set; }
    public ChatSessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    private readonly List<ChatSessionMessage> _messages = [];
    public IReadOnlyList<ChatSessionMessage> Messages => _messages.AsReadOnly();

    private ChatSession() { }

    public static ChatSession Create(Guid customerId, string customerName, Guid? departmentId = null) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        CustomerName = customerName,
        DepartmentId = departmentId,
        Status = ChatSessionStatus.Waiting,
        CreatedAt = DateTime.UtcNow,
    };

    public void AcceptHandoff(Guid agentId)
    {
        AgentId = agentId;
        Status = ChatSessionStatus.Active;
        AcceptedAt = DateTime.UtcNow;
    }

    public void SetLinkedTicketId(Guid ticketId) => LinkedTicketId = ticketId;

    public void Close()
    {
        Status = ChatSessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }

    public ChatSessionMessage AddMessage(string senderRole, Guid? senderId, string body)
    {
        var msg = ChatSessionMessage.Create(Id, senderRole, senderId, body);
        _messages.Add(msg);
        return msg;
    }
}
