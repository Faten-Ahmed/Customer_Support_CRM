namespace CRM.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> FindByIdWithContactsAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
