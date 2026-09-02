using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record DeactivateFieldDefinitionCommand(Guid FieldId) : IRequest;

public class DeactivateFieldDefinitionCommandHandler
    : IRequestHandler<DeactivateFieldDefinitionCommand>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public DeactivateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task Handle(DeactivateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        var field = await _repo.FindByIdAsync(cmd.FieldId, ct)
            ?? throw new KeyNotFoundException($"Field definition {cmd.FieldId} not found.");
        field.Deactivate();
        await _repo.SaveChangesAsync(ct);
    }
}
