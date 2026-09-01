using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketAssignedEvent(
    Guid TicketId, Guid DepartmentId, Guid AgentId) : INotification;
