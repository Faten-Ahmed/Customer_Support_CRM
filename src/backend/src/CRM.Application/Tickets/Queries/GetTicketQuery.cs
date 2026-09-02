using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketQuery(Guid TicketId) : IRequest<TicketDetailDto>;

public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;

    public GetTicketQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<TicketDetailDto> Handle(GetTicketQuery query, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {query.TicketId} not found.");

        var departmentName = ticket.DepartmentId.HasValue
            ? await _tickets.GetDepartmentNameAsync(ticket.DepartmentId.Value, ct)
            : null;

        var categoryName = ticket.CategoryId.HasValue
            ? await _tickets.GetCategoryNameAsync(ticket.CategoryId.Value, ct)
            : null;

        return new TicketDetailDto(
            Id: ticket.Id,
            TicketNumber: ticket.TicketNumber,
            CustomerId: ticket.CustomerId,
            CustomerName: ticket.Customer?.FullName ?? "Unknown",
            Subject: ticket.Subject,
            SubjectAr: ticket.SubjectAr,
            Description: ticket.Description,
            DescriptionAr: ticket.DescriptionAr,
            Status: ticket.Status.ToString(),
            Priority: ticket.Priority.ToString(),
            Channel: ticket.Channel.ToString(),
            AssignedToUserId: ticket.AssignedToUserId,
            AssignedToName: ticket.AssignedTo is null
                ? null : $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}",
            DepartmentId: ticket.DepartmentId,
            DepartmentName: departmentName,
            CategoryId: ticket.CategoryId,
            CategoryName: categoryName,
            CustomFieldValues: ticket.CustomFieldValues,
            Sla: null,
            CreatedAt: ticket.CreatedAt,
            UpdatedAt: ticket.UpdatedAt,
            ResolvedAt: ticket.ResolvedAt,
            ClosedAt: ticket.ClosedAt);
    }
}
