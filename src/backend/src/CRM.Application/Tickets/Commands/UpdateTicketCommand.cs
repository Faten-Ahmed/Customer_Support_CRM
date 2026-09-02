using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record UpdateTicketCommand(
    Guid TicketId,
    string Subject,
    string SubjectAr,
    string Description,
    string DescriptionAr,
    TicketPriority Priority,
    Guid? CategoryId,
    Guid? DepartmentId,
    string? CustomFieldValues,
    Guid UpdatedByUserId) : IRequest<TicketDetailDto>;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;

    public UpdateTicketCommandHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<TicketDetailDto> Handle(UpdateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot edit a closed ticket.");

        ticket.UpdateDetails(
            cmd.Subject, cmd.SubjectAr, cmd.Description, cmd.DescriptionAr,
            cmd.Priority, cmd.CategoryId, cmd.DepartmentId, cmd.CustomFieldValues,
            cmd.UpdatedByUserId);

        await _tickets.SaveChangesAsync(ct);

        var departmentName = ticket.DepartmentId.HasValue
            ? await _tickets.GetDepartmentNameAsync(ticket.DepartmentId.Value, ct)
            : null;

        var categoryName = ticket.CategoryId.HasValue
            ? await _tickets.GetCategoryNameAsync(ticket.CategoryId.Value, ct)
            : null;

        return new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            ticket.Customer?.FullName ?? "Unknown",
            ticket.Subject, ticket.SubjectAr,
            ticket.Description, ticket.DescriptionAr,
            ticket.Status.ToString(),
            ticket.Priority.ToString(), ticket.Channel.ToString(),
            ticket.AssignedToUserId,
            ticket.AssignedTo is null ? null : $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}",
            ticket.DepartmentId, departmentName,
            ticket.CategoryId, categoryName,
            ticket.CustomFieldValues, null,
            ticket.CreatedAt, ticket.UpdatedAt, ticket.ResolvedAt, ticket.ClosedAt);
    }
}
