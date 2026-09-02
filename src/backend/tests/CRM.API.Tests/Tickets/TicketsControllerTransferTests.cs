using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerTransferTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Manager"));
        return client;
    }

    [Fact]
    public async Task Transfer_ValidRequest_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransferTicketCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/transfer",
            new { departmentId = Guid.NewGuid(), transferNote = "Needs specialist handling" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransferTicketCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Cannot transfer closed ticket."));
        var client = BuildClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/transfer",
            new { departmentId = Guid.NewGuid(), transferNote = "Long enough reason here" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
