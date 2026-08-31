using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record SubmitKbArticleForReviewCommand(
    Guid ArticleId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class SubmitKbArticleForReviewCommandHandler
    : IRequestHandler<SubmitKbArticleForReviewCommand>
{
    private readonly IKbArticleRepository _articles;

    public SubmitKbArticleForReviewCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(SubmitKbArticleForReviewCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isAuthor = article.CreatedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isAuthor)
            throw new UnauthorizedAccessException(
                "Only the article author or a Manager/Admin can submit for review.");

        article.SubmitForReview();

        await _articles.SaveChangesAsync(ct);
    }
}
