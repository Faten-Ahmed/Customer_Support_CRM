namespace CRM.Application.Common;

/// <summary>
/// Abstraction over Hangfire's IBackgroundJobClient, so Application layer doesn't depend on Hangfire.
/// </summary>
public interface IBackgroundJobService
{
    void EnqueueWelcomeEmail(Guid userId, string email, string tempPassword);
    void EnqueueOutboundEmail(Guid ticketId, Guid messageId);
}
