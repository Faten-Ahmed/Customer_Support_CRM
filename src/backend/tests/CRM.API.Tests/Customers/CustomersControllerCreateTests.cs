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

public class CustomersControllerCreateTests
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
    public async Task CreateCustomer_ValidBody_Returns201WithLocation()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new CustomerDto(
                     id, "John Doe", "جون دو", "john@example.com",
                     "+971501234567", null, null, false, true, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "John Doe",
            fullNameAr = "جون دو",
            email = "john@example.com",
            phone = "+971501234567"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains($"/api/v1/customers/{id}", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Email already exists."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Jane Doe",
            fullNameAr = "جين دو",
            email = "dup@example.com",
            phone = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Unauthenticated_Returns401()
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
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "X Y",
            fullNameAr = "س ص",
            email = "x@y.com"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
