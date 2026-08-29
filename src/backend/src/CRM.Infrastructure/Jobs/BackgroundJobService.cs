using CRM.Application.Admin.Users.Jobs;
using CRM.Application.Common;
using Hangfire;

namespace CRM.Infrastructure.Jobs;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _jobClient;

    public BackgroundJobService(IBackgroundJobClient jobClient) => _jobClient = jobClient;

    public void EnqueueWelcomeEmail(Guid userId, string email, string tempPassword)
        => _jobClient.Enqueue<SendWelcomeEmailJob>(
            job => job.Execute(userId, email, tempPassword, CancellationToken.None));
}
