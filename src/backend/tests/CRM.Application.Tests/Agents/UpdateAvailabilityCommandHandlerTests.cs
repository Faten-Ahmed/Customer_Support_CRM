using CRM.Application.Agents.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class UpdateAvailabilityCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly UpdateAvailabilityCommandHandler _handler;

    public UpdateAvailabilityCommandHandlerTests()
    {
        _handler = new UpdateAvailabilityCommandHandler(_repo.Object);
    }

    [Theory]
    [InlineData(AvailabilityStatus.Available)]
    [InlineData(AvailabilityStatus.Busy)]
    [InlineData(AvailabilityStatus.Away)]
    [InlineData(AvailabilityStatus.Offline)]
    public async Task Handle_ValidStatus_UpdatesUser(AvailabilityStatus status)
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Agent", "One", "agent@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateAvailabilityCommand(user.Id, status), default);

        Assert.Equal(status.ToString(), result.AvailabilityStatus);
        Assert.NotNull(result.LastAvailabilityChange);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new UpdateAvailabilityCommand(Guid.NewGuid(), AvailabilityStatus.Available),
                default));
    }
}
