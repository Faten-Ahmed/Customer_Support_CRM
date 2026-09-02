using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketStatusChangedEvent(
    Guid TicketId, Guid DepartmentId, string OldStatus, string NewStatus) : INotification;
