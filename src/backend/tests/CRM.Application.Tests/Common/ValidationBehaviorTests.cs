using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using CRM.Application.Auth.Validators;
using CRM.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Xunit;

namespace CRM.Application.Tests.Common;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var validators = new List<IValidator<LoginInternalCommand>>
        {
            new LoginInternalCommandValidator()
        };
        var behavior = new ValidationBehavior<LoginInternalCommand, LoginResponse>(validators);

        var nextCalled = false;
        var response = new LoginResponse("t", "r", false, Guid.NewGuid(), "ali@crm.test", "Ali", "Hassan", "Agent");
        var next = new RequestHandlerDelegate<LoginResponse>(_ =>
        {
            nextCalled = true;
            return Task.FromResult(response);
        });

        var result = await behavior.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), next, default);

        Assert.True(nextCalled);
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        var validators = new List<IValidator<LoginInternalCommand>>
        {
            new LoginInternalCommandValidator()
        };
        var behavior = new ValidationBehavior<LoginInternalCommand, LoginResponse>(validators);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new LoginInternalCommand("not-an-email", "short"),
                _ => Task.FromResult(new LoginResponse("", "", false, Guid.NewGuid(), "", "", "", "")),
                default));
    }
}
