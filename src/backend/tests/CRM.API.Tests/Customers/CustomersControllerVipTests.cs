using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerVipTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
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
    public async Task SetVip_AsManager_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SetCustomerVipCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var client = BuildClient();
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetVip_AsAgent_Returns403()
    {
        var client = BuildClient(role: "Agent");
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetVip_NonExistentCustomer_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SetCustomerVipCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
