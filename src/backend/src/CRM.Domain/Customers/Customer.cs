namespace CRM.Domain.Customers;

public class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? CompanyName { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private Customer() { }

    public static Customer Create(string fullName, string email, string? phone, string? companyName)
        => new()
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Phone = phone,
            CompanyName = companyName,
            EmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void SetPassword(string hash) => PasswordHash = hash;
    public void VerifyEmail() => EmailVerified = true;
    public void Deactivate() => IsActive = false;
}
