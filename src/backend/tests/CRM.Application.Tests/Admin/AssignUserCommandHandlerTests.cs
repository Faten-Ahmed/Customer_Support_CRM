using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class AssignUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<ICategoryExistenceChecker> _cats = new();
    private readonly AssignUserDepartmentsCommandHandler _deptHandler;
    private readonly AssignUserSkillsCommandHandler _skillHandler;

    public AssignUserCommandHandlerTests()
    {
        _deptHandler = new AssignUserDepartmentsCommandHandler(_repo.Object);
        _skillHandler = new AssignUserSkillsCommandHandler(_repo.Object, _cats.Object);
    }

    [Fact]
    public async Task AssignDepartments_ExactlyOnePrimary_Succeeds()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true),
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: false)
        };

        await _deptHandler.Handle(
            new AssignUserDepartmentsCommand(user.Id, depts), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Equal(2, user.Departments.Count);
        Assert.Single(user.Departments, d => d.IsPrimary);
    }

    [Fact]
    public async Task AssignDepartments_MultiplePrimary_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true),
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deptHandler.Handle(
                new AssignUserDepartmentsCommand(user.Id, depts), default));

        Assert.Contains("MULTIPLE_PRIMARY_DEPARTMENTS", ex.Message);
    }

    [Fact]
    public async Task AssignDepartments_NoPrimary_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: false)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deptHandler.Handle(
                new AssignUserDepartmentsCommand(user.Id, depts), default));

        Assert.Contains("primary", ex.Message.ToLower());
    }

    [Fact]
    public async Task AssignSkills_ValidCategoryIds_Succeeds()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);
        var catId = Guid.NewGuid();
        _cats.Setup(c => c.AllExistAsync(new[] { catId }, default)).ReturnsAsync(true);

        await _skillHandler.Handle(
            new AssignUserSkillsCommand(user.Id, new[] { catId }), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Single(user.Skills);
    }

    [Fact]
    public async Task AssignSkills_UnknownCategoryId_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);
        var catId = Guid.NewGuid();
        _cats.Setup(c => c.AllExistAsync(new[] { catId }, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _skillHandler.Handle(
                new AssignUserSkillsCommand(user.Id, new[] { catId }), default));
    }

    [Fact]
    public async Task AssignSkills_EmptyList_ClearsAllSkills()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        await _skillHandler.Handle(
            new AssignUserSkillsCommand(user.Id, Array.Empty<Guid>()), default);

        Assert.Empty(user.Skills);
    }
}
