using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Events;
using MediatR;

namespace CRM.Application.Tickets.Jobs;

public class AutoCloseResolvedTicketsJob
{
    private static readonly TimeSpan AutoCloseAfter = TimeSpan.FromHours(48);

    private readonly ITicketRepository _tickets;
    private readonly IPublisher _publisher;

    public AutoCloseResolvedTicketsJob(ITicketRepository tickets, IPublisher publisher)
    {
        _tickets = tickets;
        _publisher = publisher;
    }

    public async Task Execute(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - AutoCloseAfter;
        var eligible = await _tickets.FindResolvedWithNoCustomerReplyAsync(cutoff, ct);

        if (!eligible.Any()) return;

        foreach (var ticket in eligible)
        {
            ticket.AutoClose();
            await _publisher.Publish(new TicketClosedEvent(
                ticket.Id,
                ticket.AssignedToUserId ?? Guid.Empty,
                ticket.DepartmentId ?? Guid.Empty), ct);
        }

        await _tickets.SaveChangesAsync(ct);
    }
}
