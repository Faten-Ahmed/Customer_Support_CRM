using CRM.Application.Auth.DTOs;
using CRM.Application.Auth.Queries;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsCurrentUserDto()
    {
        var userId = Guid.NewGuid();
        var user = User.CreateForTest(
            email: "manager@crm.test",
            passwordHash: "hash",
            role: UserRole.Manager,
            isActive: true,
            requiresPasswordChange: false,
            id: userId,
            firstName: "Sara",
            lastName: "Al-Ali");

        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        var result = await _handler.Handle(new GetCurrentUserQuery(userId), default);

        Assert.Equal(userId, result.Id);
        Assert.Equal("manager@crm.test", result.Email);
        Assert.Equal("Manager", result.Role);
        Assert.Equal("Sara", result.FirstName);
        Assert.Equal("Al-Ali", result.LastName);
        Assert.False(result.RequiresPasswordChange);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCurrentUserQuery(Guid.NewGuid()), default));
    }
}
