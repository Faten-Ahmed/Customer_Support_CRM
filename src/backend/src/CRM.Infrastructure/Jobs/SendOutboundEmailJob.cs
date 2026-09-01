using CRM.Application.Common;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Jobs;

public class SendOutboundEmailJob
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketMessageRepository _messages;
    private readonly ICustomerRepository _customers;
    private readonly IEmailService _email;
    private readonly ILogger<SendOutboundEmailJob> _logger;

    public SendOutboundEmailJob(
        ITicketRepository tickets,
        ITicketMessageRepository messages,
        ICustomerRepository customers,
        IEmailService email,
        ILogger<SendOutboundEmailJob> logger)
    {
        _tickets = tickets;
        _messages = messages;
        _customers = customers;
        _email = email;
        _logger = logger;
    }

    public async Task Execute(Guid ticketId, Guid messageId, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId, ct);
        if (ticket is null)
        {
            _logger.LogWarning("SendOutboundEmailJob: ticket {TicketId} not found.", ticketId);
            return;
        }

        var message = await _messages.FindByIdAsync(messageId, ct);
        if (message is null)
        {
            _logger.LogWarning("SendOutboundEmailJob: message {MessageId} not found.", messageId);
            return;
        }

        var customer = await _customers.FindByIdAsync(ticket.CustomerId, ct);
        if (customer is null)
        {
            _logger.LogWarning("SendOutboundEmailJob: customer {CustomerId} not found.", ticket.CustomerId);
            return;
        }

        await _email.SendTicketReplyAsync(
            customer.Email,
            customer.FullName,
            ticket.TicketNumber,
            ticket.Subject,
            message.Body,
            ct);

        _logger.LogInformation(
            "Outbound email sent for ticket {TicketNumber} to {Email}.",
            ticket.TicketNumber, customer.Email);
    }
}
