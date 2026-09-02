using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.Queries;
using CRM.Domain.Categories;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class CategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly CreateCategoryCommandHandler _createHandler;
    private readonly DeactivateCategoryCommandHandler _deactivateHandler;
    private readonly ListCategoriesQueryHandler _listHandler;

    public CategoryCommandHandlerTests()
    {
        _createHandler = new CreateCategoryCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateCategoryCommandHandler(_repo.Object, _tickets.Object);
        _listHandler = new ListCategoriesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_ParentCategory_Succeeds()
    {
        var result = await _createHandler.Handle(
            new CreateCategoryCommand("Technical Support", "الدعم الفني", null, 1),
            default);

        Assert.Equal("Technical Support", result.Name);
        Assert.Null(result.ParentCategoryId);
        _repo.Verify(r => r.AddAsync(It.IsAny<TicketCategory>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_ChildOfChild_ThrowsInvalidOperationException()
    {
        var grandchild = Guid.NewGuid();
        _repo.Setup(r => r.IsChildCategoryAsync(grandchild, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateCategoryCommand("Grandchild", null, grandchild, 1),
                default));
    }

    [Fact]
    public async Task Deactivate_Parent_CascadesToChildren()
    {
        var parent = TicketCategory.Create("Technical Support", null, null, 1);
        var child = TicketCategory.Create("Hardware", null, parent.Id, 1);
        _repo.Setup(r => r.FindByIdAsync(parent.Id, default)).ReturnsAsync(parent);
        _repo.Setup(r => r.GetChildrenAsync(parent.Id, default))
             .ReturnsAsync(new List<TicketCategory> { child });
        _tickets.Setup(t => t.CountOpenForCategoryAsync(parent.Id, default)).ReturnsAsync(0);
        _tickets.Setup(t => t.CountOpenForCategoryAsync(child.Id, default)).ReturnsAsync(0);

        await _deactivateHandler.Handle(
            new DeactivateCategoryCommand(parent.Id), default);

        Assert.False(parent.IsActive);
        Assert.False(child.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_ThrowsInvalidOperationException()
    {
        var category = TicketCategory.Create("Technical Support", null, null, 1);
        _repo.Setup(r => r.FindByIdAsync(category.Id, default)).ReturnsAsync(category);
        _repo.Setup(r => r.GetChildrenAsync(category.Id, default))
             .ReturnsAsync(new List<TicketCategory>());
        _tickets.Setup(t => t.CountOpenForCategoryAsync(category.Id, default)).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateCategoryCommand(category.Id), default));

        Assert.Contains("5", ex.Message);
    }
}
