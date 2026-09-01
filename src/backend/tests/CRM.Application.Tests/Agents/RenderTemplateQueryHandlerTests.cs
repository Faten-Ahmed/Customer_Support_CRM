using CRM.Application.Agents.Queries;
using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class RenderTemplateQueryHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _templates = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly RenderTemplateQueryHandler _handler;

    public RenderTemplateQueryHandlerTests()
    {
        _handler = new RenderTemplateQueryHandler(
            _templates.Object, _tickets.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AllTokens_SubstitutesCorrectly()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Support",
            "دعم",
            "Hello {{customer_name}}, your ticket {{ticket_number}} from {{department}} " +
            "is handled by {{agent_name}}.",
            "مرحبا {{customer_name}}",
            "Greeting", agentId);

        var ticketContext = new TicketRenderContext(
            "TKT-2025-00043", "Sara Al-Mansouri", "Ahmed Hassan", "IT Support");

        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(ticketContext);

        var result = await _handler.Handle(
            new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId), default);

        Assert.Equal(
            "Hello Sara Al-Mansouri, your ticket TKT-2025-00043 from IT Support " +
            "is handled by Ahmed Hassan.",
            result);
    }

    [Fact]
    public async Task Handle_UnknownToken_LeavesAsIs()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Custom", "مخصص", "Hello {{customer_name}} and {{unknown_token}}.", "مرحبا", "Cat", agentId);

        var ticketContext = new TicketRenderContext(
            "TKT-001", "Sara", "Ahmed", "IT");

        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(ticketContext);

        var result = await _handler.Handle(
            new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId), default);

        Assert.Contains("{{unknown_token}}", result);
        Assert.Contains("Sara", result);
    }

    [Fact]
    public async Task Handle_TemplateNotFound_ThrowsKeyNotFoundException()
    {
        _templates.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                  .ReturnsAsync((QuickReplyTemplate?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RenderTemplateQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsKeyNotFoundException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "T", "ت", "Content", "محتوى", null, agentId);
        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync((TicketRenderContext?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId),
                default));
    }
}
