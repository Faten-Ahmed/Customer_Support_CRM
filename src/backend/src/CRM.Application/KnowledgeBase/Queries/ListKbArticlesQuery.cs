using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.Common;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
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
    private readonly IKbCategoryRepository _categories;
    private readonly IUserRepository _users;

    public ListKbArticlesQueryHandler(
        IKbArticleRepository articles,
        IKbCategoryRepository categories,
        IUserRepository users)
    {
        _articles = articles;
        _categories = categories;
        _users = users;
    }

    public async Task<PagedResult<KbArticleSummaryDto>> Handle(
        ListKbArticlesQuery query, CancellationToken ct)
    {
        var filter = query.IsPortalCaller
            ? new KbArticleFilter(KbArticleStatus.Published, query.Filter?.CategoryId,
                KbVisibility.Public)
            : query.Filter ?? new KbArticleFilter(null, null, null);

        var paged = await _articles.ListAsync(filter, query.Page, query.PageSize, ct);

        var cats = await _categories.ListActiveAsync(ct);
        var catMap = cats.ToDictionary(c => c.Id, c => c.Name);

        var authorIds = paged.Items.Select(a => a.CreatedByUserId).Distinct();
        var users = await _users.FindByIdsAsync(authorIds, ct);
        var userMap = users.ToDictionary(
            u => u.Id,
            u => $"{u.FirstName} {u.LastName}");

        var dtos = paged.Items
            .Select(a => new KbArticleSummaryDto(
                a.Id, a.Title, a.TitleAr,
                a.CategoryId, catMap.GetValueOrDefault(a.CategoryId),
                a.Status.ToString(), a.Visibility.ToString(),
                a.CreatedByUserId, userMap.GetValueOrDefault(a.CreatedByUserId),
                a.PublishedAt, a.CreatedAt))
            .ToList();

        return new PagedResult<KbArticleSummaryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
