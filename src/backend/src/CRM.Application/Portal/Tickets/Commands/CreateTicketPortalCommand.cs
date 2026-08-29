using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Portal.Tickets.Commands;

public record CreateTicketPortalCommand(
    string Subject,
    string SubjectAr,
    string Description,
    string DescriptionAr,
    TicketPriority Priority,
    Guid? DepartmentId,
    Guid? CategoryId,
    string? CustomFieldValues,
    Guid PortalCustomerId) : IRequest<TicketSummaryDto>;

public class CreateTicketPortalCommandHandler
    : IRequestHandler<CreateTicketPortalCommand, TicketSummaryDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerCredentialRepository _credentials;
    private readonly ITicketRepository _tickets;

    public CreateTicketPortalCommandHandler(
        ICustomerRepository customers,
        ICustomerCredentialRepository credentials,
        ITicketRepository tickets)
    {
        _customers = customers;
        _credentials = credentials;
        _tickets = tickets;
    }

    public async Task<TicketSummaryDto> Handle(
        CreateTicketPortalCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.PortalCustomerId, ct);
        if (customer is null || !customer.IsActive)
            throw new KeyNotFoundException("Customer not found.");

        var credential = await _credentials.FindByCustomerIdAsync(cmd.PortalCustomerId, ct);
        if (credential is null || !credential.EmailVerified)
            throw new UnauthorizedAccessException("Email not verified. Please verify your email first.");

        var ticket = Ticket.Create(
            customerId: cmd.PortalCustomerId,
            subject: cmd.Subject,
            subjectAr: cmd.SubjectAr,
            description: cmd.Description,
            descriptionAr: cmd.DescriptionAr,
            priority: cmd.Priority,
            channel: TicketChannel.Portal,
            createdByUserId: cmd.PortalCustomerId,
            departmentId: cmd.DepartmentId,
            categoryId: cmd.CategoryId,
            customFieldValues: cmd.CustomFieldValues);

        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);

        return new TicketSummaryDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            customer.FullName,
            ticket.Subject, ticket.Status.ToString(), ticket.Priority.ToString(),
            ticket.Channel.ToString(), null, null, ticket.CreatedAt, ticket.UpdatedAt);
    }
}
