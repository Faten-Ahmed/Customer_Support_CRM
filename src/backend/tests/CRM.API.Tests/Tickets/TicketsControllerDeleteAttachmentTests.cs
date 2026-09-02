using System.Net;
using CRM.Application.Tickets.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerDeleteAttachmentTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task DeleteAttachment_Authorized_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteAttachmentCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        var client = BuildClient("Admin");

        var response = await client.DeleteAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAttachment_NotOwner_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteAttachmentCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new UnauthorizedAccessException("Not uploader."));
        var client = BuildClient("Agent");

        var response = await client.DeleteAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAttachment_NotFound_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteAttachmentCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Attachment not found."));
        var client = BuildClient("Admin");

        var response = await client.DeleteAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
