using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class GetKbArticleQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly GetKbArticleQueryHandler _handler;

    public GetKbArticleQueryHandlerTests()
    {
        _handler = new GetKbArticleQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_ReturnsAnyStatusArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var result = await _handler.Handle(
            new GetKbArticleQuery(article.Id, false), default);

        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task Handle_PortalCaller_InternalArticle_ThrowsUnauthorizedException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        article.Approve();

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new GetKbArticleQuery(article.Id, true), default));
    }

    [Fact]
    public async Task Handle_PortalCaller_NonPublishedArticle_ThrowsKeyNotFoundException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Public, new string('x', 150));

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetKbArticleQuery(article.Id, true), default));
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetKbArticleQuery(Guid.NewGuid(), false), default));
    }
}
