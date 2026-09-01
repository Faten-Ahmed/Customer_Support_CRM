namespace CRM.Application.Common;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string toEmail,
        string toName,
        string verificationToken,
        CancellationToken ct = default);

    Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetToken,
        CancellationToken ct = default);

    Task SendTicketReplyAsync(
        string toEmail,
        string toName,
        string ticketNumber,
        string subject,
        string body,
        CancellationToken ct = default);
}
