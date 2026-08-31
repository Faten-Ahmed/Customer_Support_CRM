using CRM.Domain.KnowledgeBase;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record RejectKbArticleCommand(Guid ArticleId, string RejectionNote) : IRequest;

public class RejectKbArticleCommandHandler : IRequestHandler<RejectKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public RejectKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(RejectKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        if (string.IsNullOrWhiteSpace(cmd.RejectionNote) || cmd.RejectionNote.Length < 10)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(cmd.RejectionNote),
                    "Rejection note must be at least 10 characters.")
            });

        article.Reject(cmd.RejectionNote);

        await _articles.SaveChangesAsync(ct);
    }
}
