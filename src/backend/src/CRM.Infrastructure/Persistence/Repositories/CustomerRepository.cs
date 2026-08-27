using CRM.Domain.Common;
using CRM.Domain.Customers;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub implementation until US-BE-009 adds the Customer EF table and full repository.
public class CustomerRepository : ICustomerRepository
{
    public Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Customer?>(null);

    public Task<Customer?> FindByIdWithContactsAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Customer?>(null);

    public Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult<Customer?>(null);

    public Task AddAsync(Customer customer, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<PagedResult<CustomerSummaryProjection>> ListAsync(CustomerFilter filter, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<CustomerSummaryProjection>(
            new List<CustomerSummaryProjection>(), 0, filter.Page, filter.PageSize));
}
