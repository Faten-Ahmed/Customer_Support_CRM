using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record ToggleBranchCommand(Guid BranchId, bool Activate) : IRequest<BranchActiveResult>;

public class ToggleBranchCommandHandler : IRequestHandler<ToggleBranchCommand, BranchActiveResult>
{
    private readonly IBranchRepository _branches;
    public ToggleBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchActiveResult> Handle(ToggleBranchCommand cmd, CancellationToken ct)
    {
        var branch = await _branches.FindByIdAsync(cmd.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {cmd.BranchId} not found.");
        if (cmd.Activate) branch.Reactivate(); else branch.Deactivate();
        await _branches.SaveChangesAsync(ct);
        return new BranchActiveResult(branch.Id, branch.IsActive);
    }
}
