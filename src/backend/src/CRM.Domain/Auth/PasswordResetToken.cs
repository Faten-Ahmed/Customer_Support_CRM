namespace CRM.Domain.Auth;

public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

    public void MarkUsed() => IsUsed = true;

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
