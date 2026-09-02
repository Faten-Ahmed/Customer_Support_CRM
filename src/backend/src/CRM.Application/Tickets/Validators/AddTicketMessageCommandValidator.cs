using CRM.Application.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Tickets.Validators;

public class AddTicketMessageCommandValidator : AbstractValidator<AddTicketMessageCommand>
{
    public AddTicketMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(50000);
    }
}
