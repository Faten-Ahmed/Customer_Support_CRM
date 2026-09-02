using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.Queries;
using CRM.Domain.Departments;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class DepartmentCommandHandlerTests
{
    private readonly Mock<IDepartmentRepository> _repo = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly CreateDepartmentCommandHandler _createHandler;
    private readonly UpdateDepartmentCommandHandler _updateHandler;
    private readonly DeactivateDepartmentCommandHandler _deactivateHandler;
    private readonly ListDepartmentsQueryHandler _listHandler;

    public DepartmentCommandHandlerTests()
    {
        _createHandler = new CreateDepartmentCommandHandler(_repo.Object);
        _updateHandler = new UpdateDepartmentCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateDepartmentCommandHandler(_repo.Object, _tickets.Object);
        _listHandler = new ListDepartmentsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_UniqueName_Succeeds()
    {
        _repo.Setup(r => r.ExistsByNameAsync("Technical Support", default)).ReturnsAsync(false);

        var result = await _createHandler.Handle(
            new CreateDepartmentCommand("Technical Support", "الدعم الفني", null, null),
            default);

        Assert.Equal("Technical Support", result.Name);
        _repo.Verify(r => r.AddAsync(It.IsAny<Department>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsInvalidOperationExceptionWith409()
    {
        _repo.Setup(r => r.ExistsByNameAsync("Technical Support", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateDepartmentCommand("Technical Support", null, null, null),
                default));

        Assert.Contains("409", ex.Message);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_ThrowsInvalidOperationException()
    {
        var dept = Department.Create("Technical Support", null, null, null);
        _repo.Setup(r => r.FindByIdAsync(dept.Id, default)).ReturnsAsync(dept);
        _tickets.Setup(t => t.CountOpenForDepartmentAsync(dept.Id, default)).ReturnsAsync(3);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateDepartmentCommand(dept.Id), default));

        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public async Task Deactivate_NoOpenTickets_Succeeds()
    {
        var dept = Department.Create("Technical Support", null, null, null);
        _repo.Setup(r => r.FindByIdAsync(dept.Id, default)).ReturnsAsync(dept);
        _tickets.Setup(t => t.CountOpenForDepartmentAsync(dept.Id, default)).ReturnsAsync(0);

        var result = await _deactivateHandler.Handle(
            new DeactivateDepartmentCommand(dept.Id), default);

        Assert.False(result.IsActive);
    }
}
