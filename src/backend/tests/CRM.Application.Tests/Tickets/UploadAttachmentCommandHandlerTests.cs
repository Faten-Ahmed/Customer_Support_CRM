using CRM.Application.Common;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class UploadAttachmentCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAttachmentRepository> _attachmentRepo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly UploadAttachmentCommandHandler _handler;

    public UploadAttachmentCommandHandlerTests()
    {
        _handler = new UploadAttachmentCommandHandler(
            _ticketRepo.Object, _attachmentRepo.Object, _storage.Object);
    }

    [Fact]
    public async Task Handle_ValidFile_UploadsAndReturnsDto()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _storage.Setup(s => s.UploadAsync(
            It.IsAny<Stream>(), "screenshot.png", "image/png", default))
            .ReturnsAsync("tickets/abc/screenshot.png");
        _storage.Setup(s => s.GetPresignedUrlAsync("tickets/abc/screenshot.png", default))
                .ReturnsAsync("https://s3.example.com/file.png?sig=xxx");

        var stream = new MemoryStream(new byte[1024]);
        var result = await _handler.Handle(new UploadAttachmentCommand(
            ticketId, "screenshot.png", "image/png", stream.Length, stream, Guid.NewGuid()), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("screenshot.png", result.FileName);
        Assert.Contains("s3.example.com", result.PresignedUrl);
        _attachmentRepo.Verify(r => r.AddAsync(It.IsAny<Attachment>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UploadAttachmentCommand(
                ticketId, "big.mp4", "video/mp4",
                11L * 1024 * 1024, new MemoryStream(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DisallowedMimeType_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UploadAttachmentCommand(
                ticketId, "virus.exe", "application/x-msdownload",
                1024, new MemoryStream(new byte[1024]), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _ticketRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new UploadAttachmentCommand(
                Guid.NewGuid(), "file.pdf", "application/pdf",
                1024, new MemoryStream(), Guid.NewGuid()), default));
    }
}
