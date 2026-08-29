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

public class CustomersControllerUpdateTests
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
    public async Task Update_ExistingCustomer_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new CustomerDetailDto(
                     id, "Ahmed Al-Rashid", "أحمد الرشيد", "ali@crm.test", "+971509999999",
                     null, null, false, true, DateTime.UtcNow, new List<ContactDto>()));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/v1/customers/{id}", new
        {
            fullName = "Ahmed Al-Rashid",
            fullNameAr = "أحمد الرشيد",
            phone = "+971509999999"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentCustomer_Returns404()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/v1/customers/{id}", new
        {
            fullName = "X Y",
            fullNameAr = "س ص",
            phone = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Unauthenticated_Returns401()
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
        var response = await client.PutAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}", new
        {
            fullName = "X",
            fullNameAr = "س"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
