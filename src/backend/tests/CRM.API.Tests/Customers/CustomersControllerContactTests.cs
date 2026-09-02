using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.Commands;
using CRM.Application.Customers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerContactTests
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
    public async Task AddContact_ValidBody_Returns201()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<AddCustomerContactCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ContactDto(contactId, "Phone", "+971501234567", true));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/contacts", new
        {
            type = "Phone",
            value = "+971501234567",
            isPrimary = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddContact_NonExistentCustomer_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AddCustomerContactCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}/contacts", new
        {
            type = "Phone",
            value = "+971501234567",
            isPrimary = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddContact_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("environment", "Testing");
                b.ConfigureServices(services =>
                {
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();
                    services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
                    services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                });
            });
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}/contacts", new
        {
            type = "Phone",
            value = "+971501234567",
            isPrimary = false
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
