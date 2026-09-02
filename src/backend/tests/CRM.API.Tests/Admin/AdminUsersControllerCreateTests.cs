using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Users.Commands;
using CRM.Application.Admin.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerCreateTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Admin")
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
    public async Task Create_ValidAgent_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateInternalUserCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new UserProfileDto(
                     Guid.NewGuid(), "Ahmed", "Al-Farsi", null, null, null, null,
                     "ahmed@test.com", "Agent", true, true, "Offline", DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/users",
            new
            {
                firstName = "Ahmed",
                lastName = "Al-Farsi",
                email = "ahmed@test.com",
                password = "TempPass123!",
                role = "Agent",
                primaryDepartmentId = Guid.NewGuid()
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateInternalUserCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("409: Email exists."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/users",
            new { firstName = "X", lastName = "Y", email = "dup@test.com", password = "P",
                  role = "Agent", primaryDepartmentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAgent_Returns403()
    {
        var response = await BuildClient(role: "Agent").PostAsJsonAsync(
            "/api/v1/admin/users",
            new { firstName = "X", lastName = "Y", email = "x@test.com", password = "P",
                  role = "Agent", primaryDepartmentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
