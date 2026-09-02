using System.Net;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerAttachmentTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("environment", "Testing");
                b.ConfigureServices(services =>
                {
                    services.RemoveAll<IMediator>();
                    services.AddSingleton<IMediator>(_mediator.Object);
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();
                    services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
                    services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task UploadAttachment_ValidFile_Returns201()
    {
        var ticketId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UploadAttachmentCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new AttachmentDto(
                     Guid.NewGuid(), ticketId, null, "screenshot.png", "image/png",
                     1024, "https://s3.example.com/file.png", null, DateTime.UtcNow));

        var client = BuildClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(new MemoryStream(new byte[1024])), "file", "screenshot.png");

        var response = await client.PostAsync(
            $"/api/v1/tickets/{ticketId}/attachments", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_DisallowedType_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UploadAttachmentCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("File type not allowed."));

        var client = BuildClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(new MemoryStream(new byte[512])), "file", "virus.exe");

        var response = await client.PostAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/attachments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
