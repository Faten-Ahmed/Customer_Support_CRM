using CRM.Application.KnowledgeBase.Commands;
using CRM.Application.Notifications.Commands;
using CRM.Domain.Common;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class SubmitKbArticleForReviewCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly SubmitKbArticleForReviewCommandHandler _handler;

    public SubmitKbArticleForReviewCommandHandlerTests()
    {
        _userRepo.Setup(r => r.ListAsync(It.IsAny<UserRole?>(), null, true, null, 1, 200, default))
                 .ReturnsAsync(new PagedResult<UserSummaryProjection>([], 0, 1, 200));
        _mediator.Setup(m => m.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Guid.NewGuid());
        _handler = new SubmitKbArticleForReviewCommandHandler(_repo.Object, _userRepo.Object, _mediator.Object);
    }

    private static KbArticle MakeDraftWithContent(Guid authorId)
        => KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal, new string('x', 150));

    [Fact]
    public async Task Handle_AuthorSubmits_TransitionsToPendingReview()
    {
        var authorId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new SubmitKbArticleForReviewCommand(article.Id, authorId, UserRole.Agent), default);

        Assert.Equal(KbArticleStatus.PendingReview, article.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerSubmits_AnyArticle_Succeeds()
    {
        var authorId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new SubmitKbArticleForReviewCommand(article.Id, managerId, UserRole.Manager), default);

        Assert.Equal(KbArticleStatus.PendingReview, article.Status);
    }

    [Fact]
    public async Task Handle_DifferentAgentSubmits_ThrowsUnauthorizedException()
    {
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new SubmitKbArticleForReviewCommand(article.Id, otherId, UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_ContentTooShort_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal, "Short content.");
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new SubmitKbArticleForReviewCommand(article.Id, authorId, UserRole.Agent), default));
    }
}
