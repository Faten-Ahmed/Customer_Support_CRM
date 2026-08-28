using CRM.Domain.Customers;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;

namespace CRM.Domain.Tickets;

public class Ticket
{
    public Guid Id { get; private set; }
    public string TicketNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Subject { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketChannel Channel { get; private set; }
    public string? CustomFieldValues { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Navigation properties (populated by EF Core via Include)
    public Customer? Customer { get; private set; }
    public User? AssignedTo { get; private set; }

    private readonly List<TicketHistory> _history = new();
    public IReadOnlyList<TicketHistory> History => _history.AsReadOnly();

    private Ticket() { }

    public static Ticket Create(
        Guid customerId,
        string subject,
        string description,
        TicketPriority priority,
        TicketChannel channel,
        Guid createdByUserId,
        Guid? departmentId = null,
        Guid? categoryId = null,
        string? customFieldValues = null)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = GenerateNumber(),
            CustomerId = customerId,
            Subject = subject,
            Description = description,
            Status = TicketStatus.New,
            Priority = priority,
            Channel = channel,
            DepartmentId = departmentId,
            CategoryId = categoryId,
            CustomFieldValues = customFieldValues,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ticket._history.Add(TicketHistory.Create(
            ticket.Id, "Status", null, TicketStatus.New.ToString(), createdByUserId));

        return ticket;
    }

    public void Assign(Guid agentId, Guid changedBy)
    {
        var oldAssignee = AssignedToUserId?.ToString();
        AssignedToUserId = agentId;
        Status = TicketStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
        _history.Add(TicketHistory.Create(Id, "AssignedTo", oldAssignee, agentId.ToString(), changedBy));
        _history.Add(TicketHistory.Create(Id, "Status", TicketStatus.New.ToString(), TicketStatus.Assigned.ToString(), changedBy));
    }

    public void ChangeStatus(TicketStatus newStatus, Guid changedBy)
    {
        var oldStatus = Status.ToString();
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        if (newStatus == TicketStatus.Resolved) ResolvedAt = DateTime.UtcNow;
        if (newStatus == TicketStatus.Closed) ClosedAt = DateTime.UtcNow;
        _history.Add(TicketHistory.Create(Id, "Status", oldStatus, newStatus.ToString(), changedBy));
    }

    public void UpdateDetails(
        string subject,
        string description,
        TicketPriority priority,
        Guid? categoryId,
        Guid? departmentId,
        string? customFieldValues,
        Guid changedBy)
    {
        if (Subject != subject)
        {
            _history.Add(TicketHistory.Create(Id, "Subject", Subject, subject, changedBy));
            Subject = subject;
        }
        if (Description != description)
        {
            _history.Add(TicketHistory.Create(Id, "Description", null, "(updated)", changedBy));
            Description = description;
        }
        if (Priority != priority)
        {
            _history.Add(TicketHistory.Create(Id, "Priority", Priority.ToString(), priority.ToString(), changedBy));
            Priority = priority;
        }
        if (CategoryId != categoryId || DepartmentId != departmentId)
        {
            _history.Add(TicketHistory.Create(Id, "CategoryId",
                CategoryId?.ToString(), categoryId?.ToString(), changedBy));
            CategoryId = categoryId;
            DepartmentId = departmentId;
        }
        CustomFieldValues = customFieldValues;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Transfer(
        Guid? targetDepartmentId,
        Guid? targetAgentId,
        string reason,
        Guid transferredBy)
    {
        var oldDept = DepartmentId?.ToString();
        var oldAgent = AssignedToUserId?.ToString();

        DepartmentId = targetDepartmentId;
        AssignedToUserId = targetAgentId;
        Status = targetAgentId.HasValue ? TicketStatus.Assigned : TicketStatus.New;
        UpdatedAt = DateTime.UtcNow;

        _history.Add(TicketHistory.Create(Id, "Transfer", oldDept, targetDepartmentId?.ToString(), transferredBy));
        _history.Add(TicketHistory.Create(Id, "AssignedTo", oldAgent, targetAgentId?.ToString(), transferredBy));
        _history.Add(TicketHistory.Create(Id, "TransferReason", null, reason, transferredBy));
    }

    public void CloseByCustomer()
    {
        Status = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEscalationReason(string reason, Guid escalatedBy)
    {
        _history.Add(TicketHistory.Create(Id, "EscalationReason", null, reason, escalatedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateNumber()
        => $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
}
