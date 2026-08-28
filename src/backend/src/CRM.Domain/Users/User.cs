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

    public void SetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        RequiresPasswordChange = false;
    }

    public static User CreateSeeded(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        bool requiresPasswordChange)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            RequiresPasswordChange = requiresPasswordChange,
            CreatedAt = DateTime.UtcNow
        };

    // Factory used only in tests — allows setting arbitrary state
    public static User CreateForTest(
        string email,
        string passwordHash,
        UserRole role,
        bool isActive = true,
        bool requiresPasswordChange = false,
        Guid? id = null,
        string firstName = "Test",
        string lastName = "User")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = isActive,
            RequiresPasswordChange = requiresPasswordChange,
            CreatedAt = DateTime.UtcNow
        };
}
