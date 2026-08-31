using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record DeleteKbArticleCommand(
    Guid ArticleId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class DeleteKbArticleCommandHandler : IRequestHandler<DeleteKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public DeleteKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(DeleteKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        if (article.Status != KbArticleStatus.Draft)
            throw new InvalidOperationException(
                "Only Draft articles can be deleted. Archive the article first. [MUST_ARCHIVE_FIRST]");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isAuthor = article.CreatedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isAuthor)
            throw new UnauthorizedAccessException(
                "Only the article author or a Manager/Admin can delete this draft.");

        await _articles.RemoveAsync(article, ct);
        await _articles.SaveChangesAsync(ct);
    }
}
