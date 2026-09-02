using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record CreateFieldDefinitionCommand(
    Guid DepartmentId,
    Guid? CategoryId,
    string FieldName,
    string? FieldNameAr,
    FieldType FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int SortOrder) : IRequest<FieldDefinitionDto>;

public class CreateFieldDefinitionCommandHandler
    : IRequestHandler<CreateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly ITicketFieldDefinitionRepository _repo;

    public CreateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        CreateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        if (cmd.FieldType == FieldType.Dropdown)
        {
            int count = cmd.Options?.Count ?? 0;
            if (count < 2 || count > 20)
                throw new InvalidOperationException(
                    "Dropdown field must have between 2 and 20 options.");
        }

        var field = TicketFieldDefinition.Create(
            cmd.DepartmentId, cmd.CategoryId,
            cmd.FieldName, cmd.FieldNameAr,
            cmd.FieldType, cmd.Options, cmd.IsRequired, cmd.SortOrder);

        await _repo.AddAsync(field, ct);
        await _repo.SaveChangesAsync(ct);

        return Map(field);
    }

    internal static FieldDefinitionDto Map(TicketFieldDefinition f)
        => new(f.Id, f.DepartmentId, f.CategoryId, f.FieldName, f.FieldNameAr,
               f.FieldType.ToString(), f.Options, f.IsRequired, f.SortOrder, f.IsActive);
}
