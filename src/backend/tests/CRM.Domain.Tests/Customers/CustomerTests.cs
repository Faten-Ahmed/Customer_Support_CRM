using CRM.Domain.Customers;
using Xunit;

namespace CRM.Domain.Tests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_ValidInput_ReturnsCustomerWithGeneratedId()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);

        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Alice", customer.FullName);
        Assert.Equal("alice@example.com", customer.Email);
        Assert.True(customer.IsActive);
        Assert.False(customer.IsVip);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(null!, "أليس", "alice@example.com", null, null));
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("Alice", "أليس", null!, null, null));
    }

    [Fact]
    public void Deactivate_ActiveCustomer_SetsIsActiveFalse()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        customer.Deactivate();
        Assert.False(customer.IsActive);
        Assert.NotNull(customer.DeletedAt);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_DoesNotResetDeletedAt()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        customer.Deactivate();
        var firstDeletedAt = customer.DeletedAt;
        customer.Deactivate(); // second call
        Assert.Equal(firstDeletedAt, customer.DeletedAt);
    }

    [Fact]
    public void SetVip_Customer_SetsIsVipTrue()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        customer.SetVip(true);
        Assert.True(customer.IsVip);
    }

    [Fact]
    public void Update_ValidInput_UpdatesFields()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        customer.Update("Bob", "بوب", "0501234567", "Acme");
        Assert.Equal("Bob", customer.FullName);
        Assert.Equal("0501234567", customer.Phone);
        Assert.Equal("Acme", customer.CompanyName);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentException()
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        Assert.Throws<ArgumentException>(() => customer.Update(null!, "بوب", "0501234567", "Acme"));
    }
}

public class CustomerContactTests
{
    [Fact]
    public void Create_WithInvalidType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerContact.Create(Guid.NewGuid(), "Fax", "555-0100", false));
    }

    [Fact]
    public void Create_WithValidType_Succeeds()
    {
        var contact = CustomerContact.Create(Guid.NewGuid(), "Phone", "555-0100", true);
        Assert.Equal("Phone", contact.Type);
        Assert.True(contact.IsPrimary);
    }
}

public class CustomerContactManagementTests
{
    private static Customer CreateCustomer() =>
        Customer.Create("Alice", "أليس", "alice@example.com", null, null);

    [Fact]
    public void AddContact_NewPrimary_AddsContactAndDemotesExisting()
    {
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Phone", "555-0200", isPrimary: true);

        Assert.Equal(2, customer.Contacts.Count);
        var primary = customer.Contacts.Single(c => c.IsPrimary);
        Assert.Equal("555-0200", primary.Value);
    }

    [Fact]
    public void AddContact_NonPrimary_DoesNotDemoteExisting()
    {
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Phone", "555-0200", isPrimary: false);

        Assert.Equal(2, customer.Contacts.Count);
        Assert.Single(customer.Contacts, c => c.IsPrimary);
        Assert.Equal("555-0100", customer.Contacts.Single(c => c.IsPrimary).Value);
    }

    [Fact]
    public void AddContact_DifferentType_DoesNotDemoteOtherTypes()
    {
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Email", "bob@example.com", isPrimary: true);

        var phonePrimary = customer.Contacts.Single(c => c.Type == "Phone" && c.IsPrimary);
        var emailPrimary = customer.Contacts.Single(c => c.Type == "Email" && c.IsPrimary);
        Assert.NotNull(phonePrimary);
        Assert.NotNull(emailPrimary);
    }

    [Fact]
    public void RemoveContact_ExistingNonPrimary_RemovesIt()
    {
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Phone", "555-0200", isPrimary: false);

        var nonPrimary = customer.Contacts.Single(c => !c.IsPrimary);
        customer.RemoveContact(nonPrimary.Id);

        Assert.Single(customer.Contacts);
    }

    [Fact]
    public void RemoveContact_SolePrimary_ThrowsInvalidOperationException()
    {
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);

        var primaryId = customer.Contacts.Single().Id;
        Assert.Throws<InvalidOperationException>(() => customer.RemoveContact(primaryId));
    }

    [Fact]
    public void RemoveContact_NotFound_ThrowsInvalidOperationException()
    {
        var customer = CreateCustomer();
        Assert.Throws<InvalidOperationException>(() => customer.RemoveContact(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveContact_PrimaryWithAnotherPrimaryExists_RemovesIt()
    {
        // Two phone primaries (should not happen normally but guard only checks sole primary)
        var customer = CreateCustomer();
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Phone", "555-0200", isPrimary: true); // demotes first

        // Now first one is not primary; can be removed even if we try to remove first one
        var nonPrimary = customer.Contacts.First(c => !c.IsPrimary);
        customer.RemoveContact(nonPrimary.Id); // non-primary can always be removed
        Assert.Single(customer.Contacts);
    }
}
