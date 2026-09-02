using CRM.Application.Tickets.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketHistoryQuery(
    Guid TicketId, int Page, int PageSize) : IRequest<PagedResult<TicketHistoryEntryDto>>;

public class GetTicketHistoryQueryHandler
    : IRequestHandler<GetTicketHistoryQuery, PagedResult<TicketHistoryEntryDto>>
{
    private readonly ITicketHistoryRepository _history;

    public GetTicketHistoryQueryHandler(ITicketHistoryRepository history) => _history = history;

    public async Task<PagedResult<TicketHistoryEntryDto>> Handle(
        GetTicketHistoryQuery query, CancellationToken ct)
    {
        var paged = await _history.ListByTicketAsync(query.TicketId, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(h => new TicketHistoryEntryDto(
                h.FieldChanged, h.OldValue, h.NewValue, h.ChangedByName, h.ChangedAt))
            .ToList();

        return new PagedResult<TicketHistoryEntryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
