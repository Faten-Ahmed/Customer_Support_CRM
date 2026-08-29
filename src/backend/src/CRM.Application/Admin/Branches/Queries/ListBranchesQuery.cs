using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Queries;

public record ListBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

public class ListBranchesQueryHandler : IRequestHandler<ListBranchesQuery, IReadOnlyList<BranchDto>>
{
    private readonly IBranchRepository _branches;
    public ListBranchesQueryHandler(IBranchRepository branches) => _branches = branches;

    public async Task<IReadOnlyList<BranchDto>> Handle(ListBranchesQuery query, CancellationToken ct)
    {
        var branches = await _branches.ListAsync(ct);
        return branches.Select(CreateBranchCommandHandler.Map).ToList();
    }
}
