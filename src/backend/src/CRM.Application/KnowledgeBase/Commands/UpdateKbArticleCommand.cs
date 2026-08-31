using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record UpdateKbArticleCommand(
    Guid ArticleId,
    string Title,
    Guid CategoryId,
    KbVisibility Visibility,
    string? Content,
    string? TitleAr,
    string? ContentAr) : IRequest<KbArticleSummaryDto>;

public class UpdateKbArticleCommandHandler
    : IRequestHandler<UpdateKbArticleCommand, KbArticleSummaryDto>
{
    private readonly IKbArticleRepository _articles;

    public UpdateKbArticleCommandHandler(IKbArticleRepository articles) =>
        _articles = articles;

    public async Task<KbArticleSummaryDto> Handle(
        UpdateKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Update(cmd.Title, cmd.CategoryId, cmd.Visibility,
            cmd.Content, cmd.TitleAr, cmd.ContentAr);

        await _articles.SaveChangesAsync(ct);

        return new KbArticleSummaryDto(
            article.Id, article.Title, article.TitleAr,
            article.CategoryId, article.Status.ToString(),
            article.Visibility.ToString(), article.CreatedByUserId, article.CreatedAt);
    }
}
