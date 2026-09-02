using CRM.Application.Notifications.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Notifications;
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
    private readonly IUserRepository _users;
    private readonly IMediator _mediator;

    public SubmitKbArticleForReviewCommandHandler(
        IKbArticleRepository articles,
        IUserRepository users,
        IMediator mediator)
    {
        _articles = articles;
        _users = users;
        _mediator = mediator;
    }

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

        var title = $"Article Submitted for Review: \"{article.Title}\"";
        var body = $"A KB article \"{article.Title}\" has been submitted for review.";

        var managers = await _users.ListAsync(UserRole.Manager, null, true, null, 1, 200, ct);
        var admins = await _users.ListAsync(UserRole.Admin, null, true, null, 1, 200, ct);

        foreach (var userId in managers.Items.Concat(admins.Items).Select(u => u.Id).Distinct())
        {
            await _mediator.Send(new CreateNotificationCommand(
                userId, NotificationType.KbArticleSubmittedForReview, title, body,
                "article", article.Id), ct);
        }
    }
}
