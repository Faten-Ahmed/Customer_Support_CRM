// tests/CRM.Application.Tests/Customers/VerifyEmailCommandHandlerTests.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IEmailVerificationTokenRepository> _tokens = new();
    private readonly Mock<ICustomerCredentialRepository> _credentials = new();
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _handler = new VerifyEmailCommandHandler(_tokens.Object, _credentials.Object);
    }

    private static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Fact]
    public async Task Handle_ValidToken_VerifiesEmailAndMarksTokenUsed()
    {
        var customerId = Guid.NewGuid();
        var rawToken = "valid-token-abc";
        var tokenHash = HashToken(rawToken);

        var token = EmailVerificationToken.Create(customerId, tokenHash);
        var credential = CustomerCredential.Create(customerId, "hashed_pass");

        _tokens.Setup(r => r.FindByHashAsync(tokenHash, default)).ReturnsAsync(token);
        _credentials.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(credential);

        await _handler.Handle(new VerifyEmailCommand(rawToken), default);

        Assert.True(token.IsUsed);
        Assert.True(credential.EmailVerified);
        _tokens.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _credentials.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsKeyNotFoundException()
    {
        _tokens.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
               .ReturnsAsync((EmailVerificationToken?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new VerifyEmailCommand("bad-token"), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidOperationException()
    {
        var customerId = Guid.NewGuid();
        var expiredToken = EmailVerificationToken.Create(customerId, "hash", TimeSpan.FromMilliseconds(-1));

        _tokens.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default)).ReturnsAsync(expiredToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new VerifyEmailCommand("any-token"), default));
    }
}
