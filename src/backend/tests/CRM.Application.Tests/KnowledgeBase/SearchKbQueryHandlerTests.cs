using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class SearchKbQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly SearchKbQueryHandler _handler;

    public SearchKbQueryHandlerTests()
    {
        _handler = new SearchKbQueryHandler(_repo.Object);
    }

    private static KbArticle MakePublishedArticle(string content)
    {
        // Pad to ensure >= 100 chars so SubmitForReview does not throw
        var paddedContent = content.Length >= 100 ? content : content + new string('.', 100 - content.Length);
        var article = KbArticle.Create(Guid.NewGuid(), "Reset Password Guide", Guid.NewGuid(),
            KbVisibility.Public, paddedContent);
        article.SubmitForReview();
        article.Approve();
        return article;
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsResultsWithExcerpt()
    {
        var content = "To reset your password, navigate to the login page and click 'Forgot Password'.";
        var article = MakePublishedArticle(content);

        _repo.Setup(r => r.SearchAsync("reset", false, It.IsAny<int>(), default))
             .ReturnsAsync(new List<KbArticle> { article });

        var results = await _handler.Handle(new SearchKbQuery("reset", false), default);

        Assert.Single(results);
        Assert.NotEmpty(results[0].Excerpt);
        Assert.True(results[0].Excerpt.Length <= 200);
    }

    [Fact]
    public async Task Handle_QueryTooShort_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new SearchKbQuery("a", false), default));
    }

    [Fact]
    public async Task Handle_EmptyQuery_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new SearchKbQuery("", false), default));
    }

    [Fact]
    public async Task Handle_PortalOnlyTrue_PassesPortalOnlyToRepository()
    {
        _repo.Setup(r => r.SearchAsync("password", true, It.IsAny<int>(), default))
             .ReturnsAsync(new List<KbArticle>());

        var results = await _handler.Handle(
            new SearchKbQuery("password", true), default);

        Assert.Empty(results);
        _repo.Verify(r => r.SearchAsync("password", true, It.IsAny<int>(), default), Times.Once);
    }
}
