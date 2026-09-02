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

public class TicketsControllerUpdateTests
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
    public async Task UpdateTicket_ValidBody_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateTicketCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TicketDetailDto(
                     id, "TKT-001", Guid.NewGuid(), "Ali Hassan",
                     "New Subject", "موضوع جديد",
                     "New Desc", "وصف جديد",
                     "New", "High", "Internal",
                     null, null, null, null, null, null, null, null,
                     DateTime.UtcNow, DateTime.UtcNow, null, null));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/v1/tickets/{id}",
            new { subject = "New Subject", subjectAr = "عنوان جديد", description = "New Desc", descriptionAr = "وصف جديد", priority = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateTicketCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Cannot edit closed ticket."));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/v1/tickets/{Guid.NewGuid()}",
            new { subject = "S", subjectAr = "موضوع", description = "D", descriptionAr = "وصف", priority = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
