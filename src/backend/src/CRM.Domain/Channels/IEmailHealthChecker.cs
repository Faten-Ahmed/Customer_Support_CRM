namespace CRM.Domain.Channels;

public record EmailHealthResult(bool Connected, DateTime? LastMessageAt, string? Error);

public interface IEmailHealthChecker
{
    Task<EmailHealthResult> CheckAsync(CancellationToken ct = default);
}
