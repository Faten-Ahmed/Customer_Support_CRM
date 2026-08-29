using CRM.Domain.Sla;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record UpdateSlaPolicyCommand(
    Guid PolicyId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent) : IRequest;

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand>
{
    private readonly ISlaPolicyRepository _policies;

    public UpdateSlaPolicyCommandHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task Handle(UpdateSlaPolicyCommand cmd, CancellationToken ct)
    {
        var policy = await _policies.FindByIdAsync(cmd.PolicyId, ct)
            ?? throw new KeyNotFoundException($"SLA Policy {cmd.PolicyId} not found.");

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

        policy.Update(
            cmd.FirstResponseMinutes, cmd.ResolutionMinutes,
            cmd.WarningThresholdPercent, cmd.BreachThresholdPercent,
            cmd.CriticalBreachThresholdPercent);

        await _policies.SaveChangesAsync(ct);
    }
}
