using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record UpdateFieldDefinitionCommand(
    Guid FieldId,
    string? FieldName,
    string? FieldNameAr,
    IReadOnlyList<string>? Options,
    bool? IsRequired,
    int? SortOrder) : IRequest<FieldDefinitionDto>;

public class UpdateFieldDefinitionCommandHandler
    : IRequestHandler<UpdateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public UpdateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        UpdateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        var field = await _repo.FindByIdAsync(cmd.FieldId, ct)
            ?? throw new KeyNotFoundException($"Field definition {cmd.FieldId} not found.");

        if (cmd.Options is not null && field.FieldType == FieldType.Dropdown)
        {
            if (cmd.Options.Count < 2 || cmd.Options.Count > 20)
                throw new InvalidOperationException(
                    "Dropdown field must have between 2 and 20 options.");
        }

        field.Update(cmd.FieldName, cmd.FieldNameAr, cmd.Options, cmd.IsRequired, cmd.SortOrder);
        await _repo.SaveChangesAsync(ct);

        return CreateFieldDefinitionCommandHandler.Map(field);
    }
}
