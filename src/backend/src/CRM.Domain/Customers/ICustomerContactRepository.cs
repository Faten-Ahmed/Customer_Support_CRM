namespace CRM.Domain.Customers;

public interface ICustomerContactRepository
{
    Task<List<CustomerContact>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerContact?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(CustomerContact contact, CancellationToken ct = default);
    void Remove(CustomerContact contact);
    Task SaveChangesAsync(CancellationToken ct = default);
}
