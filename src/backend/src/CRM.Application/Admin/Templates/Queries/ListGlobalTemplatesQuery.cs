using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Queries;

public record ListGlobalTemplatesQuery(string? Search, int Page, int PageSize)
    : IRequest<PagedResult<TemplateDto>>;

public class ListGlobalTemplatesQueryHandler
    : IRequestHandler<ListGlobalTemplatesQuery, PagedResult<TemplateDto>>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public ListGlobalTemplatesQueryHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<PagedResult<TemplateDto>> Handle(
        ListGlobalTemplatesQuery query, CancellationToken ct)
    {
        // Pass Guid.Empty as agentId; filter by Global scope to return only global templates
        var paged = await _templates.ListForAgentAsync(
            Guid.Empty, TemplateScope.Global, query.Search, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(CreateGlobalTemplateCommandHandler.Map)
            .ToList();

        return new PagedResult<TemplateDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
