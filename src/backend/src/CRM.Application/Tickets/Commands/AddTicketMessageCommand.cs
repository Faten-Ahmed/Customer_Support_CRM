using CRM.Application.Common;
using CRM.Application.Notifications.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Notifications;
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
    private readonly IMediator _mediator;
    private readonly IBackgroundJobService _jobs;

    public AddTicketMessageCommandHandler(
        ITicketRepository tickets,
        ITicketMessageRepository messages,
        IUserRepository users,
        ICustomerRepository customers,
        IMediator mediator,
        IBackgroundJobService jobs)
    {
        _tickets = tickets;
        _messages = messages;
        _users = users;
        _customers = customers;
        _mediator = mediator;
        _jobs = jobs;
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

        await DispatchNotificationAsync(cmd, ticket, authorName, ct);

        if (!cmd.IsInternal && cmd.AuthorUserId.HasValue && ticket.Channel == TicketChannel.Email)
            _jobs.EnqueueOutboundEmail(ticket.Id, message.Id);

        return new TicketMessageDto(
            message.Id, message.TicketId, message.Body, message.IsInternal,
            message.AuthorUserId, authorName, message.AuthorCustomerId, message.CreatedAt);
    }

    private async Task DispatchNotificationAsync(
        AddTicketMessageCommand cmd, Ticket ticket, string? authorName, CancellationToken ct)
    {
        var ticketRef = $"#{ticket.TicketNumber}";

        if (cmd.IsInternal)
        {
            if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId != cmd.AuthorUserId)
            {
                await _mediator.Send(new CreateNotificationCommand(
                    ticket.AssignedToUserId.Value,
                    NotificationType.NewInternalNote,
                    $"Internal note on Ticket {ticketRef}",
                    $"{authorName ?? "Someone"} added an internal note on \"{ticket.Subject}\".",
                    "ticket", ticket.Id), ct);
            }
        }
        else if (cmd.AuthorUserId.HasValue)
        {
            // Agent public reply → notify customer (Customer.Id acts as their identity)
            await _mediator.Send(new CreateNotificationCommand(
                ticket.CustomerId,
                NotificationType.TicketReplyReceived,
                $"Reply on Ticket {ticketRef}",
                $"{authorName ?? "Support"} replied to your ticket \"{ticket.Subject}\".",
                "ticket", ticket.Id), ct);
        }
        else if (cmd.AuthorCustomerId.HasValue && ticket.AssignedToUserId.HasValue)
        {
            // Customer message → notify assigned agent
            await _mediator.Send(new CreateNotificationCommand(
                ticket.AssignedToUserId.Value,
                NotificationType.NewMessage,
                $"New message on Ticket {ticketRef}",
                $"{authorName ?? "Customer"} sent a new message on \"{ticket.Subject}\".",
                "ticket", ticket.Id), ct);
        }
    }
}
