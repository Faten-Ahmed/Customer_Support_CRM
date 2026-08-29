using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.Queries;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class FieldDefinitionCommandHandlerTests
{
    private readonly Mock<ITicketFieldDefinitionRepository> _repo = new();
    private readonly CreateFieldDefinitionCommandHandler _createHandler;
    private readonly UpdateFieldDefinitionCommandHandler _updateHandler;
    private readonly DeactivateFieldDefinitionCommandHandler _deactivateHandler;
    private readonly ListFieldDefinitionsQueryHandler _listHandler;

    public FieldDefinitionCommandHandlerTests()
    {
        _createHandler = new CreateFieldDefinitionCommandHandler(_repo.Object);
        _updateHandler = new UpdateFieldDefinitionCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateFieldDefinitionCommandHandler(_repo.Object);
        _listHandler = new ListFieldDefinitionsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_TextField_Succeeds()
    {
        var deptId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreateFieldDefinitionCommand(
                deptId, null, "Serial Number", "الرقم التسلسلي",
                FieldType.Text, null, false, 1),
            default);

        Assert.Equal("Serial Number", result.FieldName);
        Assert.True(result.IsActive);
        _repo.Verify(r => r.AddAsync(It.IsAny<TicketFieldDefinition>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_DropdownWithOneOption_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateFieldDefinitionCommand(
                    Guid.NewGuid(), null, "Status", null,
                    FieldType.Dropdown, new[] { "OnlyOption" }, false, 1),
                default));
    }

    [Fact]
    public async Task Create_DropdownWith21Options_ThrowsInvalidOperationException()
    {
        var options = Enumerable.Range(1, 21).Select(i => $"Option {i}").ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateFieldDefinitionCommand(
                    Guid.NewGuid(), null, "Status", null,
                    FieldType.Dropdown, options, false, 1),
                default));
    }

    [Fact]
    public async Task Deactivate_SetsIsActiveFalse()
    {
        var field = TicketFieldDefinition.Create(
            Guid.NewGuid(), null, "Serial Number", null, FieldType.Text, null, false, 1);
        _repo.Setup(r => r.FindByIdAsync(field.Id, default)).ReturnsAsync(field);

        await _deactivateHandler.Handle(
            new DeactivateFieldDefinitionCommand(field.Id), default);

        Assert.False(field.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
