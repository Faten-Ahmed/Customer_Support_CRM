using CRM.Domain.Sla;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record UpdateBusinessHoursCommand(
    Guid BusinessHoursId,
    string[] WorkDays,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TimeZone) : IRequest;

public class UpdateBusinessHoursCommandHandler : IRequestHandler<UpdateBusinessHoursCommand>
{
    private readonly IBusinessHoursRepository _repo;
    public UpdateBusinessHoursCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task Handle(UpdateBusinessHoursCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        var errors = new List<ValidationFailure>();

        if (cmd.WorkDays.Length == 0)
            errors.Add(new ValidationFailure(nameof(cmd.WorkDays),
                "At least one work day is required."));

        if (cmd.StartTime >= cmd.EndTime)
            errors.Add(new ValidationFailure(nameof(cmd.StartTime),
                "Start time must be earlier than end time."));

        try { TimeZoneInfo.FindSystemTimeZoneById(cmd.TimeZone); }
        catch (TimeZoneNotFoundException)
        {
            errors.Add(new ValidationFailure(nameof(cmd.TimeZone),
                $"'{cmd.TimeZone}' is not a valid IANA timezone."));
        }

        if (errors.Any()) throw new ValidationException(errors);

        bh.Update(cmd.WorkDays, cmd.StartTime, cmd.EndTime, cmd.TimeZone);
        await _repo.SaveChangesAsync(ct);
    }
}
