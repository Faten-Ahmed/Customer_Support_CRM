using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.Queries;
using CRM.Domain.Common;
using CRM.Domain.Templates;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class GlobalTemplateCommandHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _repo = new();
    private readonly CreateGlobalTemplateCommandHandler _createHandler;
    private readonly UpdateGlobalTemplateCommandHandler _updateHandler;
    private readonly DeleteGlobalTemplateCommandHandler _deleteHandler;
    private readonly ListGlobalTemplatesQueryHandler _listHandler;

    public GlobalTemplateCommandHandlerTests()
    {
        _createHandler = new CreateGlobalTemplateCommandHandler(_repo.Object);
        _updateHandler = new UpdateGlobalTemplateCommandHandler(_repo.Object);
        _deleteHandler = new DeleteGlobalTemplateCommandHandler(_repo.Object);
        _listHandler = new ListGlobalTemplatesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_GlobalTemplate_SetsGlobalScope()
    {
        var adminId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreateGlobalTemplateCommand(adminId, "Standard Greeting", "Hello {{customer_name}}!", "Greeting"),
            default);

        Assert.Equal("Global", result.Scope);
        Assert.Equal("Standard Greeting", result.Title);
        _repo.Verify(r => r.AddAsync(It.IsAny<QuickReplyTemplate>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_GlobalTemplate_ChangesTitle()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Old Title", "Content", "Greeting", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        var result = await _updateHandler.Handle(
            new UpdateGlobalTemplateCommand(template.Id, "New Title", null, null),
            default);

        Assert.Equal("New Title", result.Title);
    }

    [Fact]
    public async Task Update_PersonalTemplate_ThrowsInvalidOperationException()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Personal Template", "Content", "Greeting", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _updateHandler.Handle(
                new UpdateGlobalTemplateCommand(template.Id, "New Title", null, null),
                default));
    }

    [Fact]
    public async Task Delete_GlobalTemplate_RemovesIt()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Title", "Content", "Cat", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await _deleteHandler.Handle(
            new DeleteGlobalTemplateCommand(template.Id), default);

        _repo.Verify(r => r.RemoveAsync(template, default), Times.Once);
    }

    [Fact]
    public async Task Delete_PersonalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Personal", "Content", "Cat", agentId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deleteHandler.Handle(
                new DeleteGlobalTemplateCommand(template.Id), default));
    }

    [Fact]
    public async Task List_ReturnsOnlyGlobalTemplates()
    {
        _repo.Setup(r => r.ListForAgentAsync(
            It.IsAny<Guid>(), TemplateScope.Global, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<QuickReplyTemplate>(
                 new List<QuickReplyTemplate>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListGlobalTemplatesQuery(null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
