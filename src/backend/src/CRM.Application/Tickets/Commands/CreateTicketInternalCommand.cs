using CRM.Application.Sla.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record CreateTicketInternalCommand(
    Guid CustomerId,
    string Subject,
    string SubjectAr,
    string Description,
    string DescriptionAr,
    TicketPriority Priority,
    TicketChannel Channel,
    Guid CreatedByUserId,
    Guid? DepartmentId,
    Guid? CategoryId,
    string? CustomFieldValues) : IRequest<TicketSummaryDto>;

public class CreateTicketInternalCommandHandler
    : IRequestHandler<CreateTicketInternalCommand, TicketSummaryDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;
    private readonly IMediator _mediator;

    public CreateTicketInternalCommandHandler(
        ICustomerRepository customers, ITicketRepository tickets, IMediator mediator)
    {
        _customers = customers;
        _tickets = tickets;
        _mediator = mediator;
    }

    public async Task<TicketSummaryDto> Handle(
        CreateTicketInternalCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct);

        if (customer is null || !customer.IsActive)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        var ticket = Ticket.Create(
            customerId: cmd.CustomerId,
            subject: cmd.Subject,
            subjectAr: cmd.SubjectAr,
            description: cmd.Description,
            descriptionAr: cmd.DescriptionAr,
            priority: cmd.Priority,
            channel: cmd.Channel,
            createdByUserId: cmd.CreatedByUserId,
            departmentId: cmd.DepartmentId,
            categoryId: cmd.CategoryId,
            customFieldValues: cmd.CustomFieldValues);

        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);

        await _mediator.Send(new StartSlaClockCommand(ticket.Id), ct);

        return new TicketSummaryDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            customer.FullName,
            ticket.Subject, ticket.Status.ToString(), ticket.Priority.ToString(),
            ticket.Channel.ToString(), ticket.AssignedToUserId, null,
            ticket.CreatedAt, ticket.UpdatedAt);
    }
}
