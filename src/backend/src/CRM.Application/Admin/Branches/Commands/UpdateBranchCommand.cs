using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record UpdateBranchCommand(Guid BranchId, string? Name, string? NameAr) : IRequest<BranchDto>;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branches;
    public UpdateBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchDto> Handle(UpdateBranchCommand cmd, CancellationToken ct)
    {
        var branch = await _branches.FindByIdAsync(cmd.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {cmd.BranchId} not found.");
        branch.Update(cmd.Name, cmd.NameAr);
        await _branches.SaveChangesAsync(ct);
        return CreateBranchCommandHandler.Map(branch);
    }
}
