using CRM.Domain.Channels;

namespace CRM.Infrastructure.Channels;

public class TwilioHealthChecker : ITwilioHealthChecker
{
    public Task<TwilioHealthResult> CheckAsync(CancellationToken ct = default)
        => Task.FromResult(new TwilioHealthResult(false, "Twilio not yet configured."));
}
