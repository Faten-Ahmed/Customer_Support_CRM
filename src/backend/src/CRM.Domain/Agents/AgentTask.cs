namespace CRM.Domain.Agents;

public class AgentTask
{
    public Guid Id { get; private set; }
    public Guid AgentId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AgentTaskPriority Priority { get; private set; }
    public AgentTaskStatus Status { get; private set; }
    public DateTime? DueAt { get; private set; }
    public Guid? TicketId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private AgentTask() { }

    public static AgentTask Create(
        Guid agentId, string title, string? description,
        AgentTaskPriority priority, DateTime? dueAt,
        Guid? ticketId, Guid? customerId)
        => new()
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            Title = title,
            Description = description,
            Priority = priority,
            Status = AgentTaskStatus.Pending,
            DueAt = dueAt,
            TicketId = ticketId,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string? title, string? description,
        AgentTaskPriority? priority, AgentTaskStatus? status, DateTime? dueAt)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (priority is not null) Priority = priority.Value;
        if (status is not null)
        {
            Status = status.Value;
            if (status == AgentTaskStatus.Completed)
                CompletedAt = DateTime.UtcNow;
        }
        if (dueAt is not null) DueAt = dueAt;
        UpdatedAt = DateTime.UtcNow;
    }
}
