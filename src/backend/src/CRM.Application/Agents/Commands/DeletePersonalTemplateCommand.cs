using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record DeletePersonalTemplateCommand(Guid TemplateId, Guid AgentId) : IRequest;

public class DeletePersonalTemplateCommandHandler
    : IRequestHandler<DeletePersonalTemplateCommand>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public DeletePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task Handle(DeletePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope == TemplateScope.Global)
            throw new InvalidOperationException(
                "Global templates cannot be deleted via this endpoint.");

        if (template.CreatedByUserId != cmd.AgentId)
            throw new UnauthorizedAccessException(
                "You can only delete your own personal templates.");

        await _templates.RemoveAsync(template, ct);
        await _templates.SaveChangesAsync(ct);
    }
}
