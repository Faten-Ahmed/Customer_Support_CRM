using CRM.Application.Auth.Commands;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _userRepo.Object, _tokenRepo.Object, _emailService.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_SendsResetEmail()
    {
        var user = User.CreateForTest("agent@crm.test", "hash", UserRole.Agent, true, false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await _handler.Handle(new ForgotPasswordCommand("agent@crm.test"), default);

        _emailService.Verify(e => e.SendPasswordResetEmailAsync(
            "agent@crm.test", It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        _tokenRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownEmail_DoesNotSendEmail()
    {
        _userRepo.Setup(r => r.FindByEmailAsync("ghost@crm.test", default))
                 .ReturnsAsync((User?)null);

        // Silent — no error, no email (prevents enumeration)
        await _handler.Handle(new ForgotPasswordCommand("ghost@crm.test"), default);

        _emailService.Verify(e => e.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingUser_TokenExpiresInOneHour()
    {
        var user = User.CreateForTest("agent@crm.test", "hash", UserRole.Agent, true, false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        PasswordResetToken? captured = null;
        _tokenRepo.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default))
                  .Callback<PasswordResetToken, CancellationToken>((t, _) => captured = t)
                  .Returns(Task.CompletedTask);

        await _handler.Handle(new ForgotPasswordCommand("agent@crm.test"), default);

        Assert.NotNull(captured);
        Assert.True(captured!.ExpiresAt > DateTime.UtcNow.AddMinutes(55));
        Assert.True(captured.ExpiresAt < DateTime.UtcNow.AddMinutes(65));
    }
}
