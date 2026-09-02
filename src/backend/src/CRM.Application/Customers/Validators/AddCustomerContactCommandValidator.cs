using CRM.Application.Customers.Commands;
using FluentValidation;

namespace CRM.Application.Customers.Validators;

public class AddCustomerContactCommandValidator : AbstractValidator<AddCustomerContactCommand>
{
    private static readonly string[] AllowedTypes = ["Phone", "Email", "WhatsApp"];

    public AddCustomerContactCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be Phone, Email, or WhatsApp.");

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(256);

        When(x => string.Equals(x.Type, "Email", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Value)
                .EmailAddress()
                .WithMessage("Value must be a valid email address.");
        });

        When(x => string.Equals(x.Type, "Phone", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(x.Type, "WhatsApp", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Value)
                .Matches(@"^\+?[\d\s\-().]{7,20}$")
                .WithMessage("Value must be a valid phone number (7–20 characters, digits, spaces, +, -, parentheses).");
        });
    }
}
