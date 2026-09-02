using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record RenderTemplateQuery(Guid TemplateId, Guid TicketId, Guid AgentId)
    : IRequest<string>;

public class RenderTemplateQueryHandler : IRequestHandler<RenderTemplateQuery, string>
{
    private readonly IQuickReplyTemplateRepository _templates;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public RenderTemplateQueryHandler(
        IQuickReplyTemplateRepository templates,
        ITicketRepository tickets,
        IUserRepository users)
    {
        _templates = templates;
        _tickets = tickets;
        _users = users;
    }

    public async Task<string> Handle(RenderTemplateQuery query, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(query.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {query.TemplateId} not found.");

        var context = await _tickets.GetRenderContextAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {query.TicketId} not found.");

        var rendered = template.Content
            .Replace("{{customer_name}}", context.CustomerFullName)
            .Replace("{{agent_name}}", context.AgentFullName)
            .Replace("{{ticket_number}}", context.TicketNumber)
            .Replace("{{department}}", context.DepartmentName);

        return rendered;
    }
}
