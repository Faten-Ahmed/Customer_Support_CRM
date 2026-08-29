using System.Net;
using CRM.Application.Customers.Queries;
using CRM.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerListTests
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
    public async Task List_NoFilter_Returns200WithPagedResult()
    {
        var items = new List<CustomerListItemDto>
        {
            new(Guid.NewGuid(), "Ali Hassan", "علي حسن", "ali@crm.test", null, null, null, false, true, 3, DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<ListCustomersQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<CustomerListItemDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync("/api/v1/customers?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_WithSearchFilter_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListCustomersQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<CustomerListItemDto>(new List<CustomerListItemDto>(), 0, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync("/api/v1/customers?search=Ali&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_Unauthenticated_Returns401()
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
        var response = await client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
