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
}
