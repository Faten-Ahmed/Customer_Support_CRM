using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record SearchKbQuery(string Query, bool PortalOnly)
    : IRequest<IReadOnlyList<KbSearchResultDto>>;

public class SearchKbQueryHandler
    : IRequestHandler<SearchKbQuery, IReadOnlyList<KbSearchResultDto>>
{
    private const int MaxResults = 20;

    private readonly IKbArticleRepository _articles;

    public SearchKbQueryHandler(IKbArticleRepository articles) => _articles = articles;

    public async Task<IReadOnlyList<KbSearchResultDto>> Handle(
        SearchKbQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Length < 2)
            throw new ValidationException(new[]
            {
                new ValidationFailure("q",
                    "Search query must be at least 2 characters.",
                    "QUERY_TOO_SHORT")
            });

        var results = await _articles.SearchAsync(
            query.Query, query.PortalOnly, MaxResults, ct);

        return results.Select(a => new KbSearchResultDto(
            a.Id, a.Title, a.TitleAr, a.CategoryId,
            a.Visibility.ToString(), a.PublishedAt,
            BuildExcerpt(a.Content, query.Query))).ToList();
    }

    private static string BuildExcerpt(string? content, string query)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        var idx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        var start = idx > 0 ? Math.Max(0, idx - 50) : 0;
        return content.Substring(start, Math.Min(200, content.Length - start));
    }
}
