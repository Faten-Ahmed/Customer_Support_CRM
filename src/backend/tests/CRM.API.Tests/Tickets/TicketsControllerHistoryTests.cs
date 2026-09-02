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

public class TicketsControllerHistoryTests
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
    public async Task GetHistory_Returns200WithPagedEntries()
    {
        var ticketId = Guid.NewGuid();
        var items = new List<TicketHistoryEntryDto>
        {
            new("Status", "New", "Assigned", "Ali Hassan", DateTime.UtcNow.AddHours(-1))
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetTicketHistoryQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<TicketHistoryEntryDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync(
            $"/api/v1/tickets/{ticketId}/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TicketHistoryEntryDto>>();
        Assert.Equal(1, body!.TotalCount);
    }
}
