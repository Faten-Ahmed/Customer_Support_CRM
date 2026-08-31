using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class DeleteKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly DeleteKbArticleCommandHandler _handler;

    public DeleteKbArticleCommandHandlerTests()
    {
        _handler = new DeleteKbArticleCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AuthorDeletesDraftArticle_RemovesIt()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default);

        _repo.Verify(r => r.RemoveAsync(article, default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerDeletesAnyDraft_RemovesIt()
    {
        var authorId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new DeleteKbArticleCommand(article.Id, managerId, UserRole.Manager), default);

        _repo.Verify(r => r.RemoveAsync(article, default), Times.Once);
    }

    [Fact]
    public async Task Handle_OtherAgentDeletesDraft_ThrowsUnauthorizedException()
    {
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, otherId, UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_PublishedArticle_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        article.Approve();

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default));

        Assert.Contains("MUST_ARCHIVE_FIRST", ex.Message);
    }

    [Fact]
    public async Task Handle_ArchivedArticle_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        article.Archive();

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default));

        Assert.Contains("MUST_ARCHIVE_FIRST", ex.Message);
    }
}
