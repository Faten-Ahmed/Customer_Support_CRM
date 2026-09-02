namespace CRM.Domain.Channels;

public record TwilioHealthResult(bool Valid, string? Error);

public interface ITwilioHealthChecker
{
    Task<TwilioHealthResult> CheckAsync(CancellationToken ct = default);
}
