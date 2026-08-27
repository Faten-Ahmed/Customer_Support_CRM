// tests/CRM.Domain.Tests/Customers/CustomerCredentialTests.cs
using CRM.Domain.Customers;
using Xunit;

namespace CRM.Domain.Tests.Customers;

public class CustomerCredentialTests
{
    private static readonly Guid SomeCustomerId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsCredentialWithEmailUnverified()
    {
        var cred = CustomerCredential.Create(SomeCustomerId, "hashed_password");

        Assert.NotEqual(Guid.Empty, cred.Id);
        Assert.Equal(SomeCustomerId, cred.CustomerId);
        Assert.Equal("hashed_password", cred.PasswordHash);
        Assert.False(cred.EmailVerified);
    }

    [Fact]
    public void Create_WithEmptyHash_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerCredential.Create(SomeCustomerId, ""));
    }

    [Fact]
    public void VerifyEmail_SetsEmailVerifiedTrue()
    {
        var cred = CustomerCredential.Create(SomeCustomerId, "hashed_password");
        cred.VerifyEmail();
        Assert.True(cred.EmailVerified);
    }

    [Fact]
    public void SetPassword_ValidHash_UpdatesPasswordHash()
    {
        var cred = CustomerCredential.Create(SomeCustomerId, "old_hash");
        cred.SetPassword("new_hash");
        Assert.Equal("new_hash", cred.PasswordHash);
    }

    [Fact]
    public void SetPassword_EmptyHash_ThrowsArgumentException()
    {
        var cred = CustomerCredential.Create(SomeCustomerId, "old_hash");
        Assert.Throws<ArgumentException>(() => cred.SetPassword(""));
    }
}

public class EmailVerificationTokenTests
{
    private static readonly Guid SomeCustomerId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsUnusedToken()
    {
        var token = EmailVerificationToken.Create(SomeCustomerId, "sha256hash");

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.False(token.IsUsed);
        Assert.True(token.IsValid);
    }

    [Fact]
    public void Create_WithEmptyHash_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            EmailVerificationToken.Create(SomeCustomerId, ""));
    }

    [Fact]
    public void MarkUsed_ValidToken_SetsIsUsedTrue()
    {
        var token = EmailVerificationToken.Create(SomeCustomerId, "sha256hash");
        token.MarkUsed();
        Assert.True(token.IsUsed);
        Assert.False(token.IsValid);
    }

    [Fact]
    public void MarkUsed_AlreadyUsedToken_ThrowsInvalidOperationException()
    {
        var token = EmailVerificationToken.Create(SomeCustomerId, "sha256hash");
        token.MarkUsed();
        Assert.Throws<InvalidOperationException>(() => token.MarkUsed());
    }

    [Fact]
    public void IsValid_ExpiredToken_ReturnsFalse()
    {
        var token = EmailVerificationToken.Create(SomeCustomerId, "sha256hash", TimeSpan.FromMilliseconds(-1));
        Assert.False(token.IsValid);
    }
}
