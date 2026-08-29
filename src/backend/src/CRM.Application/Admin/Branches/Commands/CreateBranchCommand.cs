using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record CreateBranchCommand(string Name, string? NameAr) : IRequest<BranchDto>;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branches;
    public CreateBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchDto> Handle(CreateBranchCommand cmd, CancellationToken ct)
    {
        var branch = Branch.Create(cmd.Name, cmd.NameAr);
        await _branches.AddAsync(branch, ct);
        await _branches.SaveChangesAsync(ct);
        return Map(branch);
    }

    internal static BranchDto Map(Branch b)
        => new(b.Id, b.Name, b.NameAr, b.IsActive, b.CreatedAt);
}
