// src/CRM.Domain/Customers/EmailVerificationToken.cs
namespace CRM.Domain.Customers;

public class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public bool IsUsed { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid customerId, string tokenHash, TimeSpan? validFor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TokenHash = tokenHash,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(validFor ?? TimeSpan.FromHours(24)),
        };
    }

    public bool IsValid => !IsUsed && DateTime.UtcNow <= ExpiresAt;

    public void MarkUsed()
    {
        if (!IsValid)
            throw new InvalidOperationException("Token is no longer valid.");
        IsUsed = true;
    }
}
