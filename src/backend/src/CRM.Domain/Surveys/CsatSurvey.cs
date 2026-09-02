namespace CRM.Domain.Surveys;

public class CsatSurvey
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid AgentId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public string TicketSubject { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Sent";   // Sent | Submitted | Expired
    public int? Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => Status == "Expired" || DateTime.UtcNow > ExpiresAt;

    private CsatSurvey() { }

    public static CsatSurvey Create(
        Guid ticketId, Guid customerId, Guid agentId, Guid departmentId,
        string ticketNumber, string ticketSubject) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        CustomerId = customerId,
        AgentId = agentId,
        DepartmentId = departmentId,
        TicketNumber = ticketNumber,
        TicketSubject = ticketSubject,
        Status = "Sent",
        SentAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    public static CsatSurvey CreateExpired(
        Guid ticketId, Guid customerId, Guid agentId, Guid departmentId) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        CustomerId = customerId,
        AgentId = agentId,
        DepartmentId = departmentId,
        TicketNumber = "TKT-EXP",
        TicketSubject = "Expired",
        Status = "Expired",
        SentAt = DateTime.UtcNow.AddDays(-8),
        ExpiresAt = DateTime.UtcNow.AddDays(-1)
    };

    public void Submit(int rating, string? comment)
    {
        Rating = rating;
        Comment = comment;
        Status = "Submitted";
        SubmittedAt = DateTime.UtcNow;
    }

    public void Expire() => Status = "Expired";
}
