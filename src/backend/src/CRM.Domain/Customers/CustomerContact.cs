namespace CRM.Domain.Customers;

public class CustomerContact
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Type { get; private set; } = string.Empty; // Phone, Email, WhatsApp
    public string Value { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }

    private CustomerContact() { }

    public static CustomerContact Create(Guid customerId, string type, string value, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Contact type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Contact value is required.", nameof(value));

        return new CustomerContact
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = type,
            Value = value,
            IsPrimary = isPrimary,
        };
    }

    public void DemotePrimary() => IsPrimary = false;
    public void MakePrimary() => IsPrimary = true;
}
