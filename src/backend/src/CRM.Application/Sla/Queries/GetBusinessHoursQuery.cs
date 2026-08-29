using CRM.Application.Sla.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Queries;

public record GetBusinessHoursQuery : IRequest<IReadOnlyList<BusinessHoursDto>>;

public class GetBusinessHoursQueryHandler
    : IRequestHandler<GetBusinessHoursQuery, IReadOnlyList<BusinessHoursDto>>
{
    private readonly IBusinessHoursRepository _repo;
    public GetBusinessHoursQueryHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BusinessHoursDto>> Handle(
        GetBusinessHoursQuery query, CancellationToken ct)
    {
        var records = await _repo.ListAllAsync(ct);
        return records.Select(h => new BusinessHoursDto(
            h.Id, h.DepartmentId, h.WorkDays,
            h.StartTime.ToString("HH:mm"), h.EndTime.ToString("HH:mm"),
            h.TimeZone,
            h.Holidays.Select(hol => new HolidayDto(
                hol.Id, hol.Date.ToString("yyyy-MM-dd"), hol.Name)).ToList()
        )).ToList();
    }
}
