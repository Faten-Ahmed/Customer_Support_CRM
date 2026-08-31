using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record GetKbArticleQuery(Guid ArticleId, bool IsPortalCaller)
    : IRequest<KbArticleDetailDto>;

public class GetKbArticleQueryHandler : IRequestHandler<GetKbArticleQuery, KbArticleDetailDto>
{
    private readonly IKbArticleRepository _articles;

    public GetKbArticleQueryHandler(IKbArticleRepository articles) => _articles = articles;

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

        return Map(article);
    }

    private static KbArticleDetailDto Map(KbArticle a)
        => new(a.Id, a.Title, a.TitleAr, a.Content, a.ContentAr,
               a.CategoryId, a.Status.ToString(), a.Visibility.ToString(),
               a.CreatedByUserId, a.PublishedAt, a.RejectionNote,
               a.CreatedAt, a.UpdatedAt);
}
