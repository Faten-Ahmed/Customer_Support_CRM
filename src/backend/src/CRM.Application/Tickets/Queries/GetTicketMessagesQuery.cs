using CRM.Application.Tickets.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketMessagesQuery(
    Guid TicketId,
    int Page,
    int PageSize,
    bool IsCallerCustomer) : IRequest<PagedResult<TicketMessageDto>>;

public class GetTicketMessagesQueryHandler
    : IRequestHandler<GetTicketMessagesQuery, PagedResult<TicketMessageDto>>
{
    private readonly ITicketMessageRepository _messages;

    public GetTicketMessagesQueryHandler(ITicketMessageRepository messages)
        => _messages = messages;

    public async Task<PagedResult<TicketMessageDto>> Handle(
        GetTicketMessagesQuery query, CancellationToken ct)
    {
        var paged = await _messages.ListByTicketAsync(
            query.TicketId, !query.IsCallerCustomer, query.Page, query.PageSize, ct);

        var items = paged.Items
            .Select(m => new TicketMessageDto(
                m.Id, m.TicketId, m.Body, m.IsInternal,
                m.AuthorUserId, m.AuthorName, m.AuthorCustomerId, m.CreatedAt))
            .ToList();

        return new PagedResult<TicketMessageDto>(items, paged.TotalCount, query.Page, query.PageSize);
    }
}
