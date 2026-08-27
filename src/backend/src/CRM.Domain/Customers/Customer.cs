namespace CRM.Domain.Customers;

public class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? CompanyName { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool IsVip { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public List<CustomerContact> Contacts { get; private set; } = new();

    private Customer() { }

    public static Customer Create(string fullName, string email, string? phone, string? companyName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Phone = phone,
            CompanyName = companyName,
            EmailVerified = false,
            IsVip = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string fullName, string? phone, string? companyName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        FullName = fullName;
        Phone = phone;
        CompanyName = companyName;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
    }

    public void SetVip(bool isVip) => IsVip = isVip;

    public void SetPassword(string hash) => PasswordHash = hash;
    public void VerifyEmail() => EmailVerified = true;
}
