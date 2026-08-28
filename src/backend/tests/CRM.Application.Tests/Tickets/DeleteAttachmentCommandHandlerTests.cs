using CRM.Application.Common;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class DeleteAttachmentCommandHandlerTests
{
    private readonly Mock<IAttachmentRepository> _repo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly DeleteAttachmentCommandHandler _handler;

    public DeleteAttachmentCommandHandlerTests()
    {
        _handler = new DeleteAttachmentCommandHandler(_repo.Object, _storage.Object);
    }

    private static Attachment MakeAttachment(Guid uploadedBy)
        => Attachment.Create(Guid.NewGuid(), null, "file.png", "image/png",
            1024, "tickets/file.png", uploadedBy);

    [Fact]
    public async Task Handle_AdminDeletes_RemovesRegardlessOfUploader()
    {
        var attachment = MakeAttachment(Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await _handler.Handle(new DeleteAttachmentCommand(
            attachment.TicketId, attachment.Id,
            Guid.NewGuid(), UserRole.Admin), default);

        _storage.Verify(s => s.DeleteAsync("tickets/file.png", default), Times.Once);
        _repo.Verify(r => r.Remove(attachment), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentDeletesOwnAttachment_Removes()
    {
        var agentId = Guid.NewGuid();
        var attachment = MakeAttachment(agentId);
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await _handler.Handle(new DeleteAttachmentCommand(
            attachment.TicketId, attachment.Id, agentId, UserRole.Agent), default);

        _repo.Verify(r => r.Remove(attachment), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentDeletesOtherAgentAttachment_ThrowsUnauthorized()
    {
        var attachment = MakeAttachment(Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new DeleteAttachmentCommand(
                attachment.TicketId, attachment.Id,
                Guid.NewGuid(), UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_AttachmentNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Attachment?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new DeleteAttachmentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UserRole.Agent), default));
    }
}
