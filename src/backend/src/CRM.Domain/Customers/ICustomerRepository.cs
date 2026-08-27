using CRM.Application.Common;

namespace CRM.Domain.Customers;

public record CustomerFilter(
    string? Search,
    bool? IsVip,
    bool? IsActive,
    int Page,
    int PageSize,
    string? SortBy,
    bool SortDesc);

public record CustomerSummaryProjection(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    bool IsVip,
    bool IsActive,
    int TicketCount,
    DateTime CreatedAt);

public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> FindByIdWithContactsAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<PagedResult<CustomerSummaryProjection>> ListAsync(CustomerFilter filter, CancellationToken ct = default);
}
