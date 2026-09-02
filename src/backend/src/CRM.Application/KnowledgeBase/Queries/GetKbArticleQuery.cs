using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record GetKbArticleQuery(Guid ArticleId, bool IsPortalCaller)
    : IRequest<KbArticleDetailDto>;

public class GetKbArticleQueryHandler : IRequestHandler<GetKbArticleQuery, KbArticleDetailDto>
{
    private readonly IKbArticleRepository _articles;
    private readonly IKbCategoryRepository _categories;

    public GetKbArticleQueryHandler(
        IKbArticleRepository articles, IKbCategoryRepository categories)
    {
        _articles = articles;
        _categories = categories;
    }

    public async Task<KbArticleDetailDto> Handle(GetKbArticleQuery query, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(query.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {query.ArticleId} not found.");

        if (query.IsPortalCaller)
        {
            if (article.Status != KbArticleStatus.Published)
                throw new KeyNotFoundException($"KB Article {query.ArticleId} not found.");

            if (article.Visibility == KbVisibility.Internal)
                throw new UnauthorizedAccessException(
                    "This article is for internal use only.");
        }

        var category = await _categories.FindByIdAsync(article.CategoryId, ct);

        return new KbArticleDetailDto(
            article.Id, article.Title, article.TitleAr,
            article.Content, article.ContentAr,
            article.CategoryId, category?.Name,
            article.Status.ToString(), article.Visibility.ToString(),
            article.CreatedByUserId, article.PublishedAt,
            article.RejectionNote, article.CreatedAt, article.UpdatedAt);
    }
}
