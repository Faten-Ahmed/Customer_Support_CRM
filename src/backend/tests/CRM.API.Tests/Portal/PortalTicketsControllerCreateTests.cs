using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Portal.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalTicketsControllerCreateTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAttachmentRepository> _attachments = new();
    private readonly Mock<IStorageService> _storage = new();

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
                    services.RemoveAll<ITicketRepository>();
                    services.AddSingleton<ITicketRepository>(_tickets.Object);
                    services.RemoveAll<IAttachmentRepository>();
                    services.AddSingleton<IAttachmentRepository>(_attachments.Object);
                    services.RemoveAll<IStorageService>();
                    services.AddSingleton<IStorageService>(_storage.Object);
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
                "Bearer", TestJwtHelper.CreatePortalCustomerToken());
        return client;
    }

    [Fact]
    public async Task CreatePortalTicket_ValidBody_Returns201()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketPortalCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TicketSummaryDto(id, "TKT-001", Guid.NewGuid(),
                     "Ali Hassan", "Screen black", "New", "Medium", "Portal",
                     null, null, DateTime.UtcNow, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/portal/tickets",
            new { subject = "Screen black", subjectAr = "شاشتي سوداء", description = "My screen is black", descriptionAr = "شاشتي تظهر سوداء", priority = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePortalTicket_UnverifiedEmail_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketPortalCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new UnauthorizedAccessException("Email not verified."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/portal/tickets",
            new { subject = "Subj", subjectAr = "موضوع", description = "Desc", descriptionAr = "وصف", priority = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
