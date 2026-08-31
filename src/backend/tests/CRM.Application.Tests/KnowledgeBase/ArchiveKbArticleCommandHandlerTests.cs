using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ArchiveKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly ArchiveKbArticleCommandHandler _handler;

    public ArchiveKbArticleCommandHandlerTests()
    {
        _handler = new ArchiveKbArticleCommandHandler(_repo.Object);
    }

    [Theory]
    [InlineData(KbArticleStatus.Draft)]
    [InlineData(KbArticleStatus.PendingReview)]
    [InlineData(KbArticleStatus.Published)]
    public async Task Handle_AnyNonArchivedStatus_Archives(KbArticleStatus startStatus)
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));

        if (startStatus == KbArticleStatus.PendingReview) article.SubmitForReview();
        if (startStatus == KbArticleStatus.Published)
        {
            article.SubmitForReview();
            article.Approve();
        }

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(new ArchiveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Archived, article.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyArchived_SucceedsIdempotently()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        article.Archive();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(new ArchiveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Archived, article.Status);
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new ArchiveKbArticleCommand(Guid.NewGuid()), default));
    }
}
