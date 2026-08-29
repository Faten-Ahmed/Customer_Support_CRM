using CRM.Domain.Channels;

namespace CRM.Infrastructure.Channels;

public class EmailHealthChecker : IEmailHealthChecker
{
    public Task<EmailHealthResult> CheckAsync(CancellationToken ct = default)
        => Task.FromResult(new EmailHealthResult(false, null, "Email channel not yet configured."));
}
