using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CustomerCredentialRepository : ICustomerCredentialRepository
{
    private readonly AppDbContext _context;

    public CustomerCredentialRepository(AppDbContext context) => _context = context;

    public async Task<CustomerCredential?> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _context.CustomerCredentials
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

    public async Task AddAsync(CustomerCredential credential, CancellationToken ct = default)
        => await _context.CustomerCredentials.AddAsync(credential, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
