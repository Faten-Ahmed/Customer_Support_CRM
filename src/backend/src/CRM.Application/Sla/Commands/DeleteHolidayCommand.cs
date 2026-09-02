using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record DeleteHolidayCommand(
    Guid BusinessHoursId, Guid HolidayId) : IRequest;

public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand>
{
    private readonly IBusinessHoursRepository _repo;
    public DeleteHolidayCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task Handle(DeleteHolidayCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        bh.RemoveHoliday(cmd.HolidayId);
        await _repo.SaveChangesAsync(ct);
    }
}
