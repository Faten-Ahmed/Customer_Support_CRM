using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerGetMessagesTests
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
    public async Task GetMessages_Returns200WithPagedResult()
    {
        var ticketId = Guid.NewGuid();
        var items = new List<TicketMessageDto>
        {
            new(Guid.NewGuid(), ticketId, "<p>Hi</p>", false,
                Guid.NewGuid(), "Ali Hassan", null, DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetTicketMessagesQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<TicketMessageDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync(
            $"/api/v1/tickets/{ticketId}/messages?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TicketMessageDto>>();
        Assert.Equal(1, body!.TotalCount);
    }
}
