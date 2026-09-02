using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketClosedEvent(Guid TicketId, Guid AgentId, Guid DepartmentId) : INotification;
