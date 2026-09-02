using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record ArchiveKbArticleCommand(Guid ArticleId) : IRequest;

public class ArchiveKbArticleCommandHandler : IRequestHandler<ArchiveKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public ArchiveKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(ArchiveKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Archive();

        await _articles.SaveChangesAsync(ct);
    }
}
