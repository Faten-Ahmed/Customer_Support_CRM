using CRM.Application.Admin.Templates.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record CreateGlobalTemplateCommand(
    Guid AdminId,
    string Title, string TitleAr,
    string Content, string ContentAr,
    string? Category)
    : IRequest<TemplateDto>;

public class CreateGlobalTemplateCommandHandler
    : IRequestHandler<CreateGlobalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public CreateGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        CreateGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = QuickReplyTemplate.CreateGlobal(
            cmd.Title, cmd.TitleAr, cmd.Content, cmd.ContentAr, cmd.Category, cmd.AdminId);

        await _templates.AddAsync(template, ct);
        await _templates.SaveChangesAsync(ct);

        return Map(template);
    }

    internal static TemplateDto Map(QuickReplyTemplate t)
        => new(t.Id, t.Title, t.TitleAr, t.Content, t.ContentAr,
               t.Category, t.Scope.ToString(), t.CreatedByUserId,
               t.IsActive, t.CreatedAt, t.UpdatedAt);
}
