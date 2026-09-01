using CRM.Application.Agents.DTOs;
using CRM.Application.Agents.Queries;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdatePersonalTemplateCommand(
    Guid TemplateId,
    Guid AgentId,
    string? Title,
    string? TitleAr,
    string? Content,
    string? ContentAr,
    string? Category)
    : IRequest<TemplateDto>;

public class UpdatePersonalTemplateCommandHandler
    : IRequestHandler<UpdatePersonalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public UpdatePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        UpdatePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope == TemplateScope.Global)
            throw new InvalidOperationException(
                "Global templates cannot be edited via this endpoint.");

        if (template.CreatedByUserId != cmd.AgentId)
            throw new UnauthorizedAccessException(
                "You can only edit your own personal templates.");

        template.Update(cmd.Title, cmd.TitleAr, cmd.Content, cmd.ContentAr, cmd.Category);
        await _templates.SaveChangesAsync(ct);

        return TemplateMapper.Map(template);
    }
}
