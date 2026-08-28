using CRM.Application.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Tickets.Validators;

public class CreateTicketInternalCommandValidator
    : AbstractValidator<CreateTicketInternalCommand>
{
    public CreateTicketInternalCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}
