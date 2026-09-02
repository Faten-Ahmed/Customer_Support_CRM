using CRM.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CRM.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtp, ILogger<EmailService> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string toEmail, string toName, string verificationToken, CancellationToken ct = default)
    {
        var subject = "Your CRM Portal verification code";
        var verifyUrl = $"http://localhost:4200/portal/verify-email?email={Uri.EscapeDataString(toEmail)}";
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px;">
              <h2 style="color:#1976d2;">Verify your email address</h2>
              <p>Hello {toName},</p>
              <p>Enter this 6-digit code on the verification page to activate your account:</p>
              <div style="font-size:2.5rem;font-weight:700;letter-spacing:0.4em;text-align:center;
                          background:#f5f5f5;border-radius:8px;padding:16px 0;margin:24px 0;">
                {verificationToken}
              </div>
              <p style="text-align:center;">
                <a href="{verifyUrl}" style="background:#1976d2;color:#fff;padding:12px 28px;
                   border-radius:6px;text-decoration:none;font-weight:600;">
                  Go to verification page
                </a>
              </p>
              <p style="color:#666;font-size:0.875rem;">This code expires in 24 hours. If you didn't create an account, ignore this email.</p>
            </div>
            """;

        await SendAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail, string toName, string resetToken, CancellationToken ct = default)
    {
        var subject = "Reset your CRM password";
        var resetUrl = $"http://localhost:4200/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px;">
              <h2 style="color:#1976d2;">Reset your password</h2>
              <p>Hello {toName},</p>
              <p>Click the button below to reset your password. This link expires in 1 hour.</p>
              <p style="text-align:center;margin:32px 0;">
                <a href="{resetUrl}" style="background:#1976d2;color:#fff;padding:12px 28px;
                   border-radius:6px;text-decoration:none;font-weight:600;">
                  Reset Password
                </a>
              </p>
              <p style="color:#666;font-size:0.875rem;">If you didn't request a password reset, you can safely ignore this email.</p>
            </div>
            """;

        await SendAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendTicketReplyAsync(
        string toEmail, string toName, string ticketNumber, string subject, string body,
        CancellationToken ct = default)
    {
        var emailSubject = $"Re: {subject} [#{ticketNumber}]";
        var htmlBody = $"""
            <div style="font-family:sans-serif;max-width:640px;margin:0 auto;padding:24px;">
              <p style="color:#666;font-size:0.875rem;">Ticket #{ticketNumber}</p>
              <div style="border-left:4px solid #1976d2;padding-left:16px;margin:16px 0;white-space:pre-wrap;">
                {System.Net.WebUtility.HtmlEncode(body)}
              </div>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;">
              <p style="color:#999;font-size:0.75rem;">
                This is a reply to your support ticket. Please use the customer portal to respond.
              </p>
            </div>
            """;
        await SendAsync(toEmail, toName, emailSubject, htmlBody, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
