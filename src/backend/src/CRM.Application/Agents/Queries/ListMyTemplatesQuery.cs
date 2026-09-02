using CRM.Application.Agents.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record ListMyTemplatesQuery(
    Guid AgentId,
    TemplateScope? Scope,
    string? Search,
    int Page,
    int PageSize)
    : IRequest<PagedResult<TemplateDto>>;

public class ListMyTemplatesQueryHandler
    : IRequestHandler<ListMyTemplatesQuery, PagedResult<TemplateDto>>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public ListMyTemplatesQueryHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<PagedResult<TemplateDto>> Handle(
        ListMyTemplatesQuery query, CancellationToken ct)
    {
        var paged = await _templates.ListForAgentAsync(
            query.AgentId, query.Scope, query.Search,
            query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(TemplateMapper.Map)
            .ToList();

        return new PagedResult<TemplateDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}

internal static class TemplateMapper
{
    internal static TemplateDto Map(QuickReplyTemplate t)
        => new(t.Id, t.Title, t.TitleAr, t.Content, t.ContentAr,
               t.Category, t.Scope.ToString(), t.IsActive,
               t.CreatedByUserId, t.CreatedAt, t.UpdatedAt);
}
