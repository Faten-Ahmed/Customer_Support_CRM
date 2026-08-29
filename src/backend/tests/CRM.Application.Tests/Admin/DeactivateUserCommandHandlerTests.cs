using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly DeactivateUserCommandHandler _deactivateHandler;
    private readonly ReactivateUserCommandHandler _reactivateHandler;

    public DeactivateUserCommandHandlerTests()
    {
        _deactivateHandler = new DeactivateUserCommandHandler(_repo.Object);
        _reactivateHandler = new ReactivateUserCommandHandler(_repo.Object);
    }

    private User MakeActiveAdmin(Guid? id = null)
    {
        var user = User.CreateInternal(
            id ?? Guid.NewGuid(), "Admin", "User", "admin@test.com", UserRole.Admin);
        return user;
    }

    [Fact]
    public async Task Deactivate_Self_ThrowsInvalidOperationExceptionWithCannotDeactivateSelf()
    {
        var admin = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(admin.Id, default)).ReturnsAsync(admin);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateUserCommand(admin.Id, admin.Id), default));

        Assert.Contains("CANNOT_DEACTIVATE_SELF", ex.Message);
    }

    [Fact]
    public async Task Deactivate_LastActiveAdmin_ThrowsInvalidOperationException()
    {
        var caller = MakeActiveAdmin();
        var target = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(target.Id, default)).ReturnsAsync(target);
        _repo.Setup(r => r.CountActiveAdminsAsync(default)).ReturnsAsync(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateUserCommand(target.Id, caller.Id), default));

        Assert.Contains("CANNOT_DEACTIVATE_LAST_ADMIN", ex.Message);
    }

    [Fact]
    public async Task Deactivate_NonAdminUser_Succeeds()
    {
        var caller = MakeActiveAdmin();
        var agent = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "agent@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(agent.Id, default)).ReturnsAsync(agent);

        var result = await _deactivateHandler.Handle(
            new DeactivateUserCommand(agent.Id, caller.Id), default);

        Assert.False(result.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Deactivate_SecondAdmin_Succeeds()
    {
        var caller = MakeActiveAdmin();
        var target = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(target.Id, default)).ReturnsAsync(target);
        _repo.Setup(r => r.CountActiveAdminsAsync(default)).ReturnsAsync(2);

        var result = await _deactivateHandler.Handle(
            new DeactivateUserCommand(target.Id, caller.Id), default);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Reactivate_DeactivatedUser_SetsActiveTrue()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "agent@test.com", UserRole.Agent);
        user.Deactivate();
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _reactivateHandler.Handle(
            new ReactivateUserCommand(user.Id), default);

        Assert.True(result.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
