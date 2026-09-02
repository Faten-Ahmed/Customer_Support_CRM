using CRM.Application.Sla.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Queries;

public record ListSlaPoliciesQuery : IRequest<IReadOnlyList<SlaPolicyDto>>;

public class ListSlaPoliciesQueryHandler
    : IRequestHandler<ListSlaPoliciesQuery, IReadOnlyList<SlaPolicyDto>>
{
    private readonly ISlaPolicyRepository _policies;

    public ListSlaPoliciesQueryHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task<IReadOnlyList<SlaPolicyDto>> Handle(
        ListSlaPoliciesQuery query, CancellationToken ct)
    {
        var policies = await _policies.ListAllAsync(ct);
        return policies
            .Select(p => new SlaPolicyDto(
                p.Id, p.DepartmentId, p.Priority.ToString(),
                p.FirstResponseMinutes, p.ResolutionMinutes,
                p.WarningThresholdPercent, p.BreachThresholdPercent,
                p.CriticalBreachThresholdPercent))
            .ToList();
    }
}
