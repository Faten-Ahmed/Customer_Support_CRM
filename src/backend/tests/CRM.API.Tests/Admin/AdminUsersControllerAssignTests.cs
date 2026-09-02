using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerAssignTests
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
    public async Task AssignDepartments_MultiplePrimary_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignUserDepartmentsCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException(
                     "MULTIPLE_PRIMARY_DEPARTMENTS: Exactly one department must have isPrimary = true."));

        var response = await BuildClient().PutAsJsonAsync(
            $"/api/v1/admin/users/{Guid.NewGuid()}/departments",
            new
            {
                departments = new[]
                {
                    new { departmentId = Guid.NewGuid(), isPrimary = true },
                    new { departmentId = Guid.NewGuid(), isPrimary = true }
                }
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignSkills_UnknownCategory_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignUserSkillsCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException(
                     "One or more category IDs do not exist."));

        var response = await BuildClient().PutAsJsonAsync(
            $"/api/v1/admin/users/{Guid.NewGuid()}/skills",
            new { categoryIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
