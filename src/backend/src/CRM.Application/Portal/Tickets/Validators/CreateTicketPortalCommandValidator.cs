using CRM.Application.Portal.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Portal.Tickets.Validators;

public class CreateTicketPortalCommandValidator
    : AbstractValidator<CreateTicketPortalCommand>
{
    public CreateTicketPortalCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.PortalCustomerId).NotEmpty();
    }
}
