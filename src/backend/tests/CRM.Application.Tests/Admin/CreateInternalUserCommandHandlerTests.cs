using CRM.Application.Admin.Users.Commands;
using CRM.Application.Common;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class CreateInternalUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IBackgroundJobService> _jobs = new();
    private readonly CreateInternalUserCommandHandler _handler;

    public CreateInternalUserCommandHandlerTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
        _handler = new CreateInternalUserCommandHandler(_repo.Object, _hasher.Object, _jobs.Object);
    }

    [Fact]
    public async Task Handle_AgentWithPrimaryDept_CreatesUser()
    {
        var deptId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsWithEmailAsync("agent@test.com", default)).ReturnsAsync(false);

        var result = await _handler.Handle(
            new CreateInternalUserCommand(
                "Ahmed", "Al-Farsi", "agent@test.com", "TempPass123!",
                UserRole.Agent, deptId),
            default);

        Assert.Equal("agent@test.com", result.Email);
        Assert.Equal("Agent", result.Role);
        _repo.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentWithoutPrimaryDept_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateInternalUserCommand(
                    "Ahmed", "Al-Farsi", "agent@test.com", "Pass1!", UserRole.Agent, null),
                default));
    }

    [Fact]
    public async Task Handle_AdminWithoutPrimaryDept_Succeeds()
    {
        _repo.Setup(r => r.ExistsWithEmailAsync("admin@test.com", default)).ReturnsAsync(false);

        var result = await _handler.Handle(
            new CreateInternalUserCommand(
                "Admin", "User", "admin@test.com", "Pass1!", UserRole.Admin, null),
            default);

        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _repo.Setup(r => r.ExistsWithEmailAsync("existing@test.com", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateInternalUserCommand(
                    "Test", "User", "existing@test.com", "Pass1!", UserRole.Agent, Guid.NewGuid()),
                default));

        Assert.Contains("409", ex.Message);
    }
}
