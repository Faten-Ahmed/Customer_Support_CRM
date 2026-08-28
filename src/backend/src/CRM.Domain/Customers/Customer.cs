namespace CRM.Domain.Customers;

public class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? CompanyName { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool IsVip { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    private readonly List<CustomerContact> _contacts = new();
    public IReadOnlyCollection<CustomerContact> Contacts => _contacts.AsReadOnly();

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

    public void UpdateProfile(string? fullName, string? phone, string? city)
    {
        if (fullName is not null) FullName = fullName;
        if (phone is not null) Phone = phone;
        if (city is not null) City = city;
    }

    public void UpdateCity(string city) => City = city;

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive) return;
        IsActive = true;
        DeletedAt = null;
    }

    public void SetVip(bool isVip) => IsVip = isVip;

    public void SetPassword(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        PasswordHash = hash;
    }
    public void VerifyEmail() => EmailVerified = true;

    public CustomerContact AddContact(string type, string value, bool isPrimary)
    {
        if (isPrimary)
        {
            foreach (var existing in _contacts.Where(c => c.Type == type))
                existing.DemotePrimary();
        }

        var contact = CustomerContact.Create(Id, type, value, isPrimary);
        _contacts.Add(contact);
        return contact;
    }

    public void RemoveContact(Guid contactId)
    {
        var contact = _contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new InvalidOperationException($"Contact {contactId} not found.");

        if (contact.IsPrimary)
        {
            var othersOfSameType = _contacts.Where(c => c.Type == contact.Type && c.Id != contactId).ToList();
            if (!othersOfSameType.Any(c => c.IsPrimary))
                throw new InvalidOperationException("Cannot remove the sole primary contact of its type.");
        }

        _contacts.Remove(contact);
    }
}
