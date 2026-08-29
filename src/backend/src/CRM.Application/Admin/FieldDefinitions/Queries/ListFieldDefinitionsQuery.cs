using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Queries;

public record ListFieldDefinitionsQuery(
    Guid? DepartmentId, Guid? CategoryId) : IRequest<IReadOnlyList<FieldDefinitionDto>>;

public class ListFieldDefinitionsQueryHandler
    : IRequestHandler<ListFieldDefinitionsQuery, IReadOnlyList<FieldDefinitionDto>>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public ListFieldDefinitionsQueryHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<IReadOnlyList<FieldDefinitionDto>> Handle(
        ListFieldDefinitionsQuery query, CancellationToken ct)
    {
        var fields = await _repo.GetActiveAsync(query.DepartmentId, query.CategoryId, ct);
        return fields.Select(CreateFieldDefinitionCommandHandler.Map).ToList();
    }
}
