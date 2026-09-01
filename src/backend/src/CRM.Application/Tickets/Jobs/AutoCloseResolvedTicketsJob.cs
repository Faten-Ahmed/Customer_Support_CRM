using CRM.Domain.Tickets;

namespace CRM.Application.Tickets.Jobs;

public class AutoCloseResolvedTicketsJob
{
    private static readonly TimeSpan AutoCloseAfter = TimeSpan.FromHours(48);

    private readonly ITicketRepository _tickets;

    public AutoCloseResolvedTicketsJob(ITicketRepository tickets) => _tickets = tickets;

    public async Task Execute(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - AutoCloseAfter;
        var eligible = await _tickets.FindResolvedWithNoCustomerReplyAsync(cutoff, ct);

        if (!eligible.Any()) return;

        foreach (var ticket in eligible)
            ticket.AutoClose();

        await _tickets.SaveChangesAsync(ct);
    }
}
