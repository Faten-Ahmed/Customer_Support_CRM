using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record DeleteGlobalTemplateCommand(Guid TemplateId) : IRequest;

public class DeleteGlobalTemplateCommandHandler : IRequestHandler<DeleteGlobalTemplateCommand>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public DeleteGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task Handle(DeleteGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope != TemplateScope.Global)
            throw new InvalidOperationException(
                "Only Global templates can be deleted via this admin endpoint.");

        await _templates.RemoveAsync(template, ct);
        await _templates.SaveChangesAsync(ct);
    }
}
