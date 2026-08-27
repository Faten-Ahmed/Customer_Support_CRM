namespace CRM.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default);
}
