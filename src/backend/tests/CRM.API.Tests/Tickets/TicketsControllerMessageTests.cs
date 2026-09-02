using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerMessageTests
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
    public async Task AddMessage_ValidBody_Returns201()
    {
        var ticketId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<AddTicketMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TicketMessageDto(
                     msgId, ticketId, "<p>Hello</p>", false,
                     Guid.NewGuid(), "Ali Hassan", null, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{ticketId}/messages",
            new { body = "<p>Hello</p>", isInternal = false });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddMessage_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AddTicketMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Cannot add messages to a closed ticket."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/messages",
            new { body = "msg", isInternal = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMessage_NonExistentTicket_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AddTicketMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Ticket not found."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}/messages",
            new { body = "msg", isInternal = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
