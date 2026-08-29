using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalAuthControllerRegisterTests
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
        return factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidBody_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RegisterCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/portal/register", new
        {
            fullName = "Ali Nasser",
            fullNameAr = "علي ناصر",
            email = "ali@portal.test",
            password = "P@ssword1!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RegisterCustomerCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Email already registered."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/portal/register", new
        {
            fullName = "Ali Nasser",
            fullNameAr = "علي ناصر",
            email = "dup@portal.test",
            password = "P@ssword1!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
