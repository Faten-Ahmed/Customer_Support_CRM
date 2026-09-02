using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.Queries;
using CRM.Domain.Branches;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class BranchCommandHandlerTests
{
    private readonly Mock<IBranchRepository> _repo = new();
    private readonly CreateBranchCommandHandler _createHandler;
    private readonly UpdateBranchCommandHandler _updateHandler;
    private readonly ToggleBranchCommandHandler _toggleHandler;
    private readonly ListBranchesQueryHandler _listHandler;

    public BranchCommandHandlerTests()
    {
        _createHandler = new CreateBranchCommandHandler(_repo.Object);
        _updateHandler = new UpdateBranchCommandHandler(_repo.Object);
        _toggleHandler = new ToggleBranchCommandHandler(_repo.Object);
        _listHandler = new ListBranchesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_Branch_Persists()
    {
        var result = await _createHandler.Handle(
            new CreateBranchCommand("Riyadh Branch", "فرع الرياض"), default);

        Assert.Equal("Riyadh Branch", result.Name);
        Assert.True(result.IsActive);
        _repo.Verify(r => r.AddAsync(It.IsAny<Branch>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_Branch_ChangesName()
    {
        var branch = Branch.Create("Riyadh Branch", "فرع الرياض");
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _updateHandler.Handle(
            new UpdateBranchCommand(branch.Id, "Jeddah Branch", "فرع جدة"), default);

        Assert.Equal("Jeddah Branch", result.Name);
    }

    [Fact]
    public async Task Deactivate_ActiveBranch_SetsInactive()
    {
        var branch = Branch.Create("Riyadh Branch", null);
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _toggleHandler.Handle(
            new ToggleBranchCommand(branch.Id, Activate: false), default);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Reactivate_InactiveBranch_SetsActive()
    {
        var branch = Branch.Create("Riyadh Branch", null);
        branch.Deactivate();
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _toggleHandler.Handle(
            new ToggleBranchCommand(branch.Id, Activate: true), default);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task List_ReturnsBranches()
    {
        _repo.Setup(r => r.ListAsync(default))
             .ReturnsAsync(new List<Branch>());

        var result = await _listHandler.Handle(new ListBranchesQuery(), default);

        Assert.Empty(result);
    }
}
