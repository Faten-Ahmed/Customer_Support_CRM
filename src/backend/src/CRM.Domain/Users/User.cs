namespace CRM.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequiresPasswordChange { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    // Factory used only in tests — allows setting arbitrary state
    public static User CreateForTest(
        string email,
        string passwordHash,
        UserRole role,
        bool isActive = true,
        bool requiresPasswordChange = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User",
            Role = role,
            IsActive = isActive,
            RequiresPasswordChange = requiresPasswordChange,
            CreatedAt = DateTime.UtcNow
        };
}
