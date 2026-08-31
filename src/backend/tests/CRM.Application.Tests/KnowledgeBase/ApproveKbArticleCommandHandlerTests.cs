using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ApproveKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly ApproveKbArticleCommandHandler _handler;

    public ApproveKbArticleCommandHandlerTests()
    {
        _handler = new ApproveKbArticleCommandHandler(_repo.Object);
    }

    private static KbArticle MakePendingReviewArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        return article;
    }

    [Fact]
    public async Task Handle_PendingReviewArticle_TransitionsToPublished()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(new ApproveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Published, article.Status);
        Assert.NotNull(article.PublishedAt);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftArticle_ThrowsInvalidOperationException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ApproveKbArticleCommand(article.Id), default));
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new ApproveKbArticleCommand(Guid.NewGuid()), default));
    }
}
