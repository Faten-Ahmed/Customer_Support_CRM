using CRM.Application.Admin.Templates.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record UpdateGlobalTemplateCommand(
    Guid TemplateId,
    string? Title, string? TitleAr,
    string? Content, string? ContentAr,
    string? Category)
    : IRequest<TemplateDto>;

public class UpdateGlobalTemplateCommandHandler
    : IRequestHandler<UpdateGlobalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public UpdateGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(UpdateGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope != TemplateScope.Global)
            throw new InvalidOperationException(
                "Only Global templates can be edited via this endpoint.");

        template.Update(cmd.Title, cmd.TitleAr, cmd.Content, cmd.ContentAr, cmd.Category);
        await _templates.SaveChangesAsync(ct);

        return CreateGlobalTemplateCommandHandler.Map(template);
    }
}
