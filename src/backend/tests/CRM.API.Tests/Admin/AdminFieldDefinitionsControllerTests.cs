using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminFieldDefinitionsControllerTests
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
    public async Task Create_TextField_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateFieldDefinitionCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new FieldDefinitionDto(
                     Guid.NewGuid(), Guid.NewGuid(), null, "Serial Number", null,
                     "Text", null, false, 1, true));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/field-definitions",
            new
            {
                departmentId = Guid.NewGuid(),
                fieldName = "Serial Number",
                fieldType = "Text",
                isRequired = false,
                sortOrder = 1
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DropdownWithOneOption_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateFieldDefinitionCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException(
                     "Dropdown field must have between 2 and 20 options."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/field-definitions",
            new
            {
                departmentId = Guid.NewGuid(),
                fieldName = "Status",
                fieldType = "Dropdown",
                options = new[] { "OnlyOption" },
                isRequired = false,
                sortOrder = 1
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeactivates_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateFieldDefinitionCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient()
            .DeleteAsync($"/api/v1/admin/field-definitions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
