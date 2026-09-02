using CRM.Application.Notifications.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Notifications;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record ApproveKbArticleCommand(Guid ArticleId) : IRequest;

public class ApproveKbArticleCommandHandler : IRequestHandler<ApproveKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;
    private readonly IMediator _mediator;

    public ApproveKbArticleCommandHandler(IKbArticleRepository articles, IMediator mediator)
    {
        _articles = articles;
        _mediator = mediator;
    }

    public async Task Handle(ApproveKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Approve();

        await _articles.SaveChangesAsync(ct);

        await _mediator.Send(new CreateNotificationCommand(
            article.CreatedByUserId,
            NotificationType.KbArticlePublished,
            $"Article Published: \"{article.Title}\"",
            $"Your KB article \"{article.Title}\" has been approved and published.",
            "article", article.Id), ct);
    }
}
