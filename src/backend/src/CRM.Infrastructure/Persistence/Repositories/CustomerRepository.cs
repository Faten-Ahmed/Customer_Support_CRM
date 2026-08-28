using CRM.Domain.Common;
using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) => _context = context;

    public async Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> FindByIdWithContactsAsync(Guid id, CancellationToken ct = default)
        => await _context.Customers
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email && c.IsActive, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
        => await _context.Customers.AddAsync(customer, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task<PagedResult<CustomerSummaryProjection>> ListAsync(
        CustomerFilter filter, CancellationToken ct = default)
    {
        var query = _context.Customers.AsQueryable();

        if (filter.IsActive.HasValue)
            query = query.Where(c => c.IsActive == filter.IsActive.Value);

        if (filter.IsVip.HasValue)
            query = query.Where(c => c.IsVip == filter.IsVip.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                (c.Phone != null && c.Phone.ToLower().Contains(s)) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }

        query = filter.SortBy?.ToLower() switch
        {
            "fullname"  => filter.SortDesc ? query.OrderByDescending(c => c.FullName)  : query.OrderBy(c => c.FullName),
            "email"     => filter.SortDesc ? query.OrderByDescending(c => c.Email)     : query.OrderBy(c => c.Email),
            "createdat" => filter.SortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _           => query.OrderByDescending(c => c.CreatedAt),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CustomerSummaryProjection(
                c.Id, c.FullName, c.Email, c.Phone, c.CompanyName,
                c.IsVip, c.IsActive, 0, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CustomerSummaryProjection>(items, totalCount, filter.Page, filter.PageSize);
    }
}
