using CRM.Application.Notifications.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Notifications;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record RejectKbArticleCommand(Guid ArticleId, string RejectionNote) : IRequest;

public class RejectKbArticleCommandHandler : IRequestHandler<RejectKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;
    private readonly IMediator _mediator;

    public RejectKbArticleCommandHandler(IKbArticleRepository articles, IMediator mediator)
    {
        _articles = articles;
        _mediator = mediator;
    }

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

        await _mediator.Send(new CreateNotificationCommand(
            article.CreatedByUserId,
            NotificationType.KbArticleRejected,
            $"Article Rejected: \"{article.Title}\"",
            $"Your KB article \"{article.Title}\" was rejected. Note: {cmd.RejectionNote}",
            "article", article.Id), ct);
    }
}
