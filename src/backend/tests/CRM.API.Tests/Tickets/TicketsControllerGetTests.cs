using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerGetTests
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
    public async Task GetTicket_Existing_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTicketQuery>(q => q.TicketId == id), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TicketDetailDto(
                     id, "TKT-001", Guid.NewGuid(), "Ali Hassan",
                     "Cannot login", "لا يمكن تسجيل الدخول",
                     "Description goes here", "وصف",
                     "New", "High", "Internal",
                     null, null, null, null, null, null, null, null,
                     DateTime.UtcNow, DateTime.UtcNow, null, null));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/v1/tickets/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketDetailDto>();
        Assert.Equal("Cannot login", body!.Subject);
    }

    [Fact]
    public async Task GetTicket_NonExistent_Returns404()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTicketQuery>(q => q.TicketId == id), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/v1/tickets/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
