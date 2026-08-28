using CRM.Application.Common;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Email;

// Stub until a real email provider (SendGrid / SMTP) is wired up.
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    public Task SendVerificationEmailAsync(
        string toEmail, string toName, string verificationToken, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DEV] Verification token for {Email}: {Token} — POST /api/v1/auth/portal/verify-email with {{\"token\":\"{Token}\"}}",
            toEmail, verificationToken, verificationToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail, string toName, string resetToken, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DEV] Password reset token for {Email}: {Token}",
            toEmail, resetToken);
        return Task.CompletedTask;
    }
}
