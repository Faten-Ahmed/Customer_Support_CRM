using MediatR;
namespace CRM.Domain.Tickets.Events;
public record AgentStatusChangedEvent(
    Guid AgentId, Guid DepartmentId, string NewStatus) : INotification;
