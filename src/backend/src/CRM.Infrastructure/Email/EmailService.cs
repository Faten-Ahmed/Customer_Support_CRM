using CRM.Application.Common;

namespace CRM.Infrastructure.Email;

// Stub until a real email provider (SendGrid / SMTP) is wired up.
public class EmailService : IEmailService
{
    public Task SendVerificationEmailAsync(
        string toEmail, string toName, string verificationToken, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendPasswordResetEmailAsync(
        string toEmail, string toName, string resetToken, CancellationToken ct = default)
        => Task.CompletedTask;
}
