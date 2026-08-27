using CRM.Domain.Customers;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub until the customer credential storage (US-BE-014) is implemented.
public class CustomerCredentialRepository : ICustomerCredentialRepository
{
    public Task<CustomerCredential?> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult<CustomerCredential?>(null);

    public Task AddAsync(CustomerCredential credential, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
