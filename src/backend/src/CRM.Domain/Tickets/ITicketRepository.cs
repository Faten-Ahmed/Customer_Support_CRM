using CRM.Domain.Common;
using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Tickets;

public record CustomerTicketProjection(
    Guid Id,
    string TicketNumber,
    string Subject,
    string Status,
    string Priority,
    DateTime CreatedAt,
    string? Category);

public record TicketListProjection(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string Status,
    string Priority,
    string Channel,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TicketFilter(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? CustomerId,
    Guid? AssignedToUserId,
    Guid? AgentQueueUserId,
    Guid? CategoryId,
    string? Search,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc);

public record AgentTicketFilter(
    string? Status,
    string? Priority,
    Guid? DepartmentId,
    string? SortBy,
    string? SortDir);

public record MyTicketProjection(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerFullName,
    string Subject,
    string Status,
    string Priority,
    string Channel,
    Guid? DepartmentId,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? ResolutionDue,
    string SlaStatus,
    int? ResolutionRemainingMinutes);

public record TicketRenderContext(
    string TicketNumber,
    string CustomerFullName,
    string AgentFullName,
    string DepartmentName);

public interface ITicketRepository
{
    Task<Ticket?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> FindByIdDetailedAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default);
    Task<PagedResult<TicketListProjection>> ListAsync(TicketFilter filter, CancellationToken ct = default);
    Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
        Guid customerId,
        string? status,
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> CountOpenForDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<int> CountOpenForCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<string?> GetDepartmentNameAsync(Guid departmentId, CancellationToken ct = default);
    Task<string?> GetCategoryNameAsync(Guid categoryId, CancellationToken ct = default);
    Task<bool> IsDepartmentActiveAsync(Guid departmentId, CancellationToken ct = default);
    Task<PagedResult<Ticket>> ListUnassignedAsync(
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> FindResolvedWithNoCustomerReplyAsync(
        DateTime resolvedBefore,
        CancellationToken ct = default);

    Task<PagedResult<MyTicketProjection>> ListAssignedToAgentAsync(
        Guid agentId,
        AgentTicketFilter filter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<TicketRenderContext?> GetRenderContextAsync(
        Guid ticketId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
