using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record AddHolidayCommand(
    Guid BusinessHoursId, DateOnly Date, string Name) : IRequest<Guid>;

public class AddHolidayCommandHandler : IRequestHandler<AddHolidayCommand, Guid>
{
    private readonly IBusinessHoursRepository _repo;
    public AddHolidayCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddHolidayCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        var holiday = bh.AddHoliday(cmd.Date, cmd.Name);
        await _repo.SaveChangesAsync(ct);
        return holiday.Id;
    }
}
