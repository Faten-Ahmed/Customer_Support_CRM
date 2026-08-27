using CRM.Domain.Common;

namespace CRM.Domain.Tickets;

public record CustomerTicketProjection(
    string TicketNumber,
    string Subject,
    string Status,
    string Priority,
    DateTime CreatedAt,
    string? Category);

public interface ITicketRepository
{
    Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default);

    Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
        Guid customerId,
        string? status,
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
