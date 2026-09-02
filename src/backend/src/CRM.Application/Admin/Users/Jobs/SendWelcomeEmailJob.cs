namespace CRM.Application.Admin.Users.Jobs;

public class SendWelcomeEmailJob
{
    public Task Execute(Guid userId, string email, string tempPassword,
        CancellationToken ct = default)
    {
        // Implemented in US-BE-088 (email channel)
        return Task.CompletedTask;
    }
}
