using CRM.Application.Agents.DTOs;
using CRM.Application.Agents.Queries;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record CreatePersonalTemplateCommand(
    Guid AgentId,
    string Title,
    string TitleAr,
    string Content,
    string ContentAr,
    string? Category)
    : IRequest<TemplateDto>;

public class CreatePersonalTemplateCommandHandler
    : IRequestHandler<CreatePersonalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public CreatePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        CreatePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = QuickReplyTemplate.CreatePersonal(
            cmd.Title, cmd.TitleAr, cmd.Content, cmd.ContentAr, cmd.Category, cmd.AgentId);

        await _templates.AddAsync(template, ct);
        await _templates.SaveChangesAsync(ct);

        return TemplateMapper.Map(template);
    }
}
