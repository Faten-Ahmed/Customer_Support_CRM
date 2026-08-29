using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record CreateSlaPolicyCommand(
    TicketPriority Priority,
    Guid? DepartmentId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent) : IRequest<Guid>;

public class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, Guid>
{
    private readonly ISlaPolicyRepository _policies;

    public CreateSlaPolicyCommandHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task<Guid> Handle(CreateSlaPolicyCommand cmd, CancellationToken ct)
    {
        var errors = new List<ValidationFailure>();

        if (cmd.FirstResponseMinutes <= 0 || cmd.FirstResponseMinutes >= cmd.ResolutionMinutes)
            errors.Add(new ValidationFailure(nameof(cmd.FirstResponseMinutes),
                "First response minutes must be > 0 and < resolution minutes."));

        if (cmd.WarningThresholdPercent >= cmd.BreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.WarningThresholdPercent),
                "Warning threshold must be less than breach threshold."));

        if (cmd.BreachThresholdPercent >= cmd.CriticalBreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.BreachThresholdPercent),
                "Breach threshold must be less than critical breach threshold."));

        if (errors.Any()) throw new ValidationException(errors);

        var policy = SlaPolicy.Create(
            cmd.Priority, cmd.FirstResponseMinutes, cmd.ResolutionMinutes,
            cmd.DepartmentId, cmd.WarningThresholdPercent,
            cmd.BreachThresholdPercent, cmd.CriticalBreachThresholdPercent);

        await _policies.AddAsync(policy, ct);
        await _policies.SaveChangesAsync(ct);

        return policy.Id;
    }
}
