using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record AddTicketMessageCommand(
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid? AuthorUserId,
    Guid? AuthorCustomerId) : IRequest<TicketMessageDto>;

public class AddTicketMessageCommandHandler
    : IRequestHandler<AddTicketMessageCommand, TicketMessageDto>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketMessageRepository _messages;
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;

    public AddTicketMessageCommandHandler(
        ITicketRepository tickets,
        ITicketMessageRepository messages,
        IUserRepository users,
        ICustomerRepository customers)
    {
        _tickets = tickets;
        _messages = messages;
        _users = users;
        _customers = customers;
    }

    public async Task<TicketMessageDto> Handle(
        AddTicketMessageCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot add messages to a closed ticket.");

        var message = TicketMessage.Create(
            cmd.TicketId, cmd.Body, cmd.IsInternal,
            cmd.AuthorUserId, cmd.AuthorCustomerId);

        await _messages.AddAsync(message, ct);
        await _messages.SaveChangesAsync(ct);

        string? authorName = null;
        if (cmd.AuthorUserId.HasValue)
        {
            var user = await _users.FindByIdAsync(cmd.AuthorUserId.Value, ct);
            if (user is not null)
                authorName = $"{user.FirstName} {user.LastName}";
        }
        else if (cmd.AuthorCustomerId.HasValue)
        {
            var customer = await _customers.FindByIdAsync(cmd.AuthorCustomerId.Value, ct);
            authorName = customer?.FullName;
        }

        return new TicketMessageDto(
            message.Id, message.TicketId, message.Body, message.IsInternal,
            message.AuthorUserId, authorName, message.AuthorCustomerId, message.CreatedAt);
    }
}
