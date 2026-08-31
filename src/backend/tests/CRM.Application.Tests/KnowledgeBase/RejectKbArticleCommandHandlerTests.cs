using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class RejectKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly RejectKbArticleCommandHandler _handler;

    public RejectKbArticleCommandHandlerTests()
    {
        _handler = new RejectKbArticleCommandHandler(_repo.Object);
    }

    private static KbArticle MakePendingReviewArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        return article;
    }

    [Fact]
    public async Task Handle_ValidNote_TransitionsBackToDraft()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new RejectKbArticleCommand(article.Id, "Please add more examples and diagrams."), default);

        Assert.Equal(KbArticleStatus.Draft, article.Status);
        Assert.NotNull(article.RejectionNote);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectionNoteTooShort_ThrowsValidationException()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new RejectKbArticleCommand(article.Id, "Too short"), default));
    }

    [Fact]
    public async Task Handle_EmptyRejectionNote_ThrowsValidationException()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new RejectKbArticleCommand(article.Id, ""), default));
    }

    [Fact]
    public async Task Handle_ArticleNotPendingReview_ThrowsInvalidOperationException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new RejectKbArticleCommand(article.Id, "This is a valid rejection note."), default));
    }
}
