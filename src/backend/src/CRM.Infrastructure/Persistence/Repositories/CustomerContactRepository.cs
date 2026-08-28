using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CustomerContactRepository : ICustomerContactRepository
{
    private readonly AppDbContext _context;

    public CustomerContactRepository(AppDbContext context) => _context = context;

    public async Task<List<CustomerContact>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _context.CustomerContacts
            .Where(c => c.CustomerId == customerId)
            .ToListAsync(ct);

    public async Task<CustomerContact?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CustomerContacts.FindAsync([id], ct);

    public async Task AddAsync(CustomerContact contact, CancellationToken ct = default)
        => await _context.CustomerContacts.AddAsync(contact, ct);

    public void Remove(CustomerContact contact)
        => _context.CustomerContacts.Remove(contact);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
