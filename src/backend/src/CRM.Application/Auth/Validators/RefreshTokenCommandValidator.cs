using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RawToken).NotEmpty();
    }
}
