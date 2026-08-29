using System.Net;
using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerRemoveContactTests
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
    public async Task RemoveContact_ExistingContact_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RemoveCustomerContactCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var client = BuildClient();
        var response = await client.DeleteAsync(
            $"/api/v1/customers/{Guid.NewGuid()}/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveContact_SolePrimary_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RemoveCustomerContactCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Cannot remove sole primary contact."));

        var client = BuildClient();
        var response = await client.DeleteAsync(
            $"/api/v1/customers/{Guid.NewGuid()}/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task RemoveContact_NonExistentContact_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RemoveCustomerContactCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Contact not found."));

        var client = BuildClient();
        var response = await client.DeleteAsync(
            $"/api/v1/customers/{Guid.NewGuid()}/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
