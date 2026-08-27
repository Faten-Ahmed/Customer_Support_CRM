// src/CRM.Domain/Customers/CustomerCredential.cs
namespace CRM.Domain.Customers;

public class CustomerCredential
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool EmailVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CustomerCredential() { }

    public static CustomerCredential Create(Guid customerId, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new CustomerCredential
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PasswordHash = passwordHash,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void VerifyEmail() => EmailVerified = true;

    public void SetPassword(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        PasswordHash = hash;
    }
}
