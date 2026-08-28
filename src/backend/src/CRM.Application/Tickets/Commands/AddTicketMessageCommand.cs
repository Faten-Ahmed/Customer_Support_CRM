using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
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

    public AddTicketMessageCommandHandler(
        ITicketRepository tickets, ITicketMessageRepository messages)
    {
        _tickets = tickets;
        _messages = messages;
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

        return new TicketMessageDto(
            message.Id, message.TicketId, message.Body, message.IsInternal,
            message.AuthorUserId, null, message.AuthorCustomerId, message.CreatedAt);
    }
}
