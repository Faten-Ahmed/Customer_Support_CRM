using CRM.Application.Admin.Users.Commands;
using CRM.Application.Admin.Users.Queries;
using CRM.Domain.Common;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class UserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly ListUsersQueryHandler _listHandler;
    private readonly GetUserQueryHandler _getHandler;
    private readonly UpdateUserCommandHandler _updateHandler;

    public UserQueryHandlerTests()
    {
        _listHandler = new ListUsersQueryHandler(_repo.Object);
        _getHandler = new GetUserQueryHandler(_repo.Object);
        _updateHandler = new UpdateUserCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task List_ReturnsPagedUsers()
    {
        _repo.Setup(r => r.ListAsync(null, null, null, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<UserSummaryProjection>(
                 new List<UserSummaryProjection>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListUsersQuery(null, null, null, null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Get_UserFound_ReturnsDetailDto()
    {
        var userId = Guid.NewGuid();
        var projection = new UserDetailProjection(
            userId, "Ahmed", "Al-Farsi", null, null, null, null,
            "ahmed@test.com", "Agent",
            true, false, "Offline", DateTimeOffset.UtcNow,
            new List<DepartmentAssignmentProjection>(),
            new List<SkillProjection>());

        _repo.Setup(r => r.GetDetailAsync(userId, default)).ReturnsAsync(projection);

        var result = await _getHandler.Handle(new GetUserQuery(userId), default);

        Assert.Equal("ahmed@test.com", result.Email);
    }

    [Fact]
    public async Task Get_UserNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetDetailAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((UserDetailProjection?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _getHandler.Handle(new GetUserQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Update_ValidUser_UpdatesName()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Old", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _updateHandler.Handle(
            new UpdateUserCommand(user.Id, "Ahmed", "Updated"), default);

        Assert.Equal("Ahmed", result.FirstName);
        Assert.Equal("Updated", result.LastName);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
