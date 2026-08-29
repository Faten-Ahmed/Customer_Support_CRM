using System.Net;
using System.Net.Http.Json;
using CRM.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerForgotPasswordTests
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
    public async Task ForgotPassword_AnyEmail_Always200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), default))
                 .Returns(Task.FromResult(MediatR.Unit.Value));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "whoever@crm.test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
