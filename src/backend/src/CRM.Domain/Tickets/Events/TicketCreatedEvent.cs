using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketCreatedEvent(Guid TicketId, Guid DepartmentId) : INotification;
