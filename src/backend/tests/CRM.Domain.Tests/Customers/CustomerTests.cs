using CRM.Domain.Customers;
using Xunit;

namespace CRM.Domain.Tests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_ValidInput_ReturnsCustomerWithGeneratedId()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);

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
            Customer.Create(null!, "alice@example.com", null, null));
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("Alice", null!, null, null));
    }

    [Fact]
    public void Deactivate_ActiveCustomer_SetsIsActiveFalse()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.Deactivate();
        Assert.False(customer.IsActive);
        Assert.NotNull(customer.DeletedAt);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_DoesNotResetDeletedAt()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.Deactivate();
        var firstDeletedAt = customer.DeletedAt;
        customer.Deactivate(); // second call
        Assert.Equal(firstDeletedAt, customer.DeletedAt);
    }

    [Fact]
    public void SetVip_Customer_SetsIsVipTrue()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetVip(true);
        Assert.True(customer.IsVip);
    }

    [Fact]
    public void Update_ValidInput_UpdatesFields()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.Update("Bob", "0501234567", "Acme");
        Assert.Equal("Bob", customer.FullName);
        Assert.Equal("0501234567", customer.Phone);
        Assert.Equal("Acme", customer.CompanyName);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentException()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        Assert.Throws<ArgumentException>(() => customer.Update(null!, "0501234567", "Acme"));
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
