using CRM.Application.Agents.Commands;
using CRM.Application.Agents.Queries;
using CRM.Domain.Common;
using CRM.Domain.Templates;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class PersonalTemplateCommandHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _repo = new();
    private readonly CreatePersonalTemplateCommandHandler _createHandler;
    private readonly UpdatePersonalTemplateCommandHandler _updateHandler;
    private readonly DeletePersonalTemplateCommandHandler _deleteHandler;
    private readonly ListMyTemplatesQueryHandler _listHandler;

    public PersonalTemplateCommandHandlerTests()
    {
        _createHandler = new CreatePersonalTemplateCommandHandler(_repo.Object);
        _updateHandler = new UpdatePersonalTemplateCommandHandler(_repo.Object);
        _deleteHandler = new DeletePersonalTemplateCommandHandler(_repo.Object);
        _listHandler = new ListMyTemplatesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_PersonalTemplate_ReturnsDto()
    {
        var agentId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreatePersonalTemplateCommand(
                agentId, "My Greeting", "ترحيب", "Hello {{customer_name}}!", "مرحبا {{customer_name}}!", "Greeting"),
            default);

        Assert.Equal("Personal", result.Scope);
        Assert.Equal("My Greeting", result.Title);
        _repo.Verify(r => r.AddAsync(It.IsAny<QuickReplyTemplate>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Update_GlobalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Global Greeting", "ترحيب عالمي", "Hello!", "مرحبا!", "Greeting", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _updateHandler.Handle(
                new UpdatePersonalTemplateCommand(
                    template.Id, agentId, "New Title", null, null, null, null),
                default));
    }

    [Fact]
    public async Task Update_OtherAgentPersonalTemplate_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "My Template", "قالبي", "Content", "محتوى", "Cat", ownerId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _updateHandler.Handle(
                new UpdatePersonalTemplateCommand(
                    template.Id, otherId, "New Title", null, null, null, null),
                default));
    }

    [Fact]
    public async Task Delete_GlobalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Global", "عالمي", "Content", "محتوى", "Cat", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deleteHandler.Handle(
                new DeletePersonalTemplateCommand(template.Id, agentId), default));
    }

    [Fact]
    public async Task List_ReturnsPersonalAndGlobalTemplates()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListForAgentAsync(agentId, null, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<QuickReplyTemplate>(
                 new List<QuickReplyTemplate>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListMyTemplatesQuery(agentId, null, null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
