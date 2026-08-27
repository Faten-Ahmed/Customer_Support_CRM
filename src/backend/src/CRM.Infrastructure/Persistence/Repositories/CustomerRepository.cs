using CRM.Domain.Customers;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub implementation until US-BE-009 adds the Customer EF table and full repository.
public class CustomerRepository : ICustomerRepository
{
    public Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult<Customer?>(null);
}
