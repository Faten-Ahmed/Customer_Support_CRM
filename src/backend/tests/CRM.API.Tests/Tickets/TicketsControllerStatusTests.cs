using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerStatusTests
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
                "Bearer", TestJwtHelper.CreateTestToken());
        return client;
    }

    [Fact]
    public async Task ChangeStatus_ValidTransition_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ChangeTicketStatusCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/status",
            new { status = (int)TicketStatus.InProgress });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ChangeTicketStatusCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Invalid transition."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/status",
            new { status = (int)TicketStatus.Resolved });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
