// src/CRM.Domain/Customers/ICustomerCredentialRepository.cs
namespace CRM.Domain.Customers;

public interface ICustomerCredentialRepository
{
    Task<CustomerCredential?> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CustomerCredential credential, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
