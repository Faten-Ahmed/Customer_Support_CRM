using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerCreateTests
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
    public async Task CreateTicket_ValidBody_Returns201()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketInternalCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TicketSummaryDto(id, "TKT-001", Guid.NewGuid(),
                     "Ali Hassan", "Cannot login", "New", "High", "Internal",
                     null, null, DateTime.UtcNow, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            customerId = Guid.NewGuid(),
            subject = "Cannot login",
            subjectAr = "لا أستطيع تسجيل الدخول",
            description = "User cannot access portal",
            descriptionAr = "المستخدم لا يستطيع الوصول للبوابة",
            priority = 2,
            channel = 5
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_NonExistentCustomer_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketInternalCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            customerId = Guid.NewGuid(),
            subject = "Subj",
            subjectAr = "موضوع",
            description = "Desc",
            descriptionAr = "وصف",
            priority = 0,
            channel = 5
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
