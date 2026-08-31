using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record ApproveKbArticleCommand(Guid ArticleId) : IRequest;

public class ApproveKbArticleCommandHandler : IRequestHandler<ApproveKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public ApproveKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(ApproveKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Approve();

        await _articles.SaveChangesAsync(ct);
    }
}
