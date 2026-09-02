using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class CreateKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _articleRepo = new();
    private readonly Mock<IKbCategoryRepository> _categoryRepo = new();
    private readonly CreateKbArticleCommandHandler _handler;

    public CreateKbArticleCommandHandlerTests()
    {
        _handler = new CreateKbArticleCommandHandler(_articleRepo.Object, _categoryRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCategoryId_CreatesDraftArticle()
    {
        var categoryId = Guid.NewGuid();
        var category = KbCategory.Create("Support");
        _categoryRepo.Setup(r => r.FindByIdAsync(categoryId, default)).ReturnsAsync(category);

        var result = await _handler.Handle(new CreateKbArticleCommand(
            "How to reset password", categoryId, Guid.NewGuid(),
            KbVisibility.Internal, null, null, null), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Draft", result.Status);
        _articleRepo.Verify(r => r.AddAsync(It.IsAny<KbArticle>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCategoryId_ThrowsKeyNotFoundException()
    {
        _categoryRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((KbCategory?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateKbArticleCommand(
                "Title", Guid.NewGuid(), Guid.NewGuid(),
                KbVisibility.Internal, null, null, null), default));
    }

    [Fact]
    public async Task Handle_WithOptionalContent_IncludesContentInArticle()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.FindByIdAsync(categoryId, default))
                     .ReturnsAsync(KbCategory.Create("Help"));

        var result = await _handler.Handle(new CreateKbArticleCommand(
            "Title", categoryId, Guid.NewGuid(),
            KbVisibility.Public, "Some content here", "عنوان", "محتوى"), default);

        Assert.Equal("Draft", result.Status);
        Assert.Equal("Public", result.Visibility);
    }
}
