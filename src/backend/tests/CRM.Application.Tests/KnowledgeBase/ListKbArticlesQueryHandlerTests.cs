using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.Common;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ListKbArticlesQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly Mock<IKbCategoryRepository> _categories = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ListKbArticlesQueryHandler _handler;

    public ListKbArticlesQueryHandlerTests()
    {
        _categories.Setup(c => c.ListActiveAsync(default))
            .ReturnsAsync(new List<KbCategory>());
        _users.Setup(u => u.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<User>());

        _handler = new ListKbArticlesQueryHandler(_repo.Object, _categories.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_AppliesProvidedFilter()
    {
        var filter = new KbArticleFilter(KbArticleStatus.Published, null, null);
        _repo.Setup(r => r.ListAsync(
            It.Is<KbArticleFilter>(f => f.Status == KbArticleStatus.Published),
            1, 20, default))
            .ReturnsAsync(new PagedResult<KbArticle>(new List<KbArticle>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListKbArticlesQuery(filter, 1, 20, false), default);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_PortalCaller_ForcesPublishedAndPublicFilter()
    {
        _repo.Setup(r => r.ListAsync(
            It.Is<KbArticleFilter>(f =>
                f.Status == KbArticleStatus.Published &&
                f.Visibility != KbVisibility.Internal),
            1, 20, default))
            .ReturnsAsync(new PagedResult<KbArticle>(new List<KbArticle>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListKbArticlesQuery(null, 1, 20, true), default);

        Assert.Equal(0, result.TotalCount);
    }
}
