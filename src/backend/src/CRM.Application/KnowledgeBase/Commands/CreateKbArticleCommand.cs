using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record CreateKbArticleCommand(
    string Title,
    Guid CategoryId,
    Guid CreatedByUserId,
    KbVisibility Visibility,
    string? Content,
    string? TitleAr,
    string? ContentAr) : IRequest<KbArticleSummaryDto>;

public class CreateKbArticleCommandHandler
    : IRequestHandler<CreateKbArticleCommand, KbArticleSummaryDto>
{
    private readonly IKbArticleRepository _articles;
    private readonly IKbCategoryRepository _categories;

    public CreateKbArticleCommandHandler(
        IKbArticleRepository articles, IKbCategoryRepository categories)
    {
        _articles = articles;
        _categories = categories;
    }

    public async Task<KbArticleSummaryDto> Handle(
        CreateKbArticleCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"KB Category {cmd.CategoryId} not found or inactive.");

        var article = KbArticle.Create(
            cmd.CategoryId, cmd.Title, cmd.CreatedByUserId,
            cmd.Visibility, cmd.Content, cmd.TitleAr, cmd.ContentAr);

        await _articles.AddAsync(article, ct);
        await _articles.SaveChangesAsync(ct);

        return new KbArticleSummaryDto(
            article.Id, article.Title, article.TitleAr,
            article.CategoryId, category.Name, article.Status.ToString(),
            article.Visibility.ToString(), article.CreatedByUserId, article.CreatedAt);
    }
}
