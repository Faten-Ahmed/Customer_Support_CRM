using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.Common;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record ListKbArticlesQuery(
    KbArticleFilter? Filter,
    int Page,
    int PageSize,
    bool IsPortalCaller) : IRequest<PagedResult<KbArticleSummaryDto>>;

public class ListKbArticlesQueryHandler
    : IRequestHandler<ListKbArticlesQuery, PagedResult<KbArticleSummaryDto>>
{
    private readonly IKbArticleRepository _articles;

    public ListKbArticlesQueryHandler(IKbArticleRepository articles) => _articles = articles;

    public async Task<PagedResult<KbArticleSummaryDto>> Handle(
        ListKbArticlesQuery query, CancellationToken ct)
    {
        var filter = query.IsPortalCaller
            ? new KbArticleFilter(KbArticleStatus.Published, query.Filter?.CategoryId,
                KbVisibility.Public)
            : query.Filter ?? new KbArticleFilter(null, null, null);

        var paged = await _articles.ListAsync(filter, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(a => new KbArticleSummaryDto(
                a.Id, a.Title, a.TitleAr, a.CategoryId,
                a.Status.ToString(), a.Visibility.ToString(),
                a.CreatedByUserId, a.CreatedAt))
            .ToList();

        return new PagedResult<KbArticleSummaryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
