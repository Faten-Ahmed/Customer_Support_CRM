using CRM.Application.CSAT.Jobs;
using CRM.Application.Notifications.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Surveys;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class SendCsatSurveyJobTests
{
    private readonly Mock<ICsatSurveyRepository> _surveys = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly SendCsatSurveyJob _job;

    public SendCsatSurveyJobTests()
    {
        _job = new SendCsatSurveyJob(
            _surveys.Object, _tickets.Object, _customers.Object, _mediator.Object);
    }

    [Fact]
    public async Task Execute_NoExistingSurvey_CreatesSurveyAndNotification()
    {
        var ticketId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create(
            customerId, "Issue", "مشكلة", "Description", "وصف",
            TicketPriority.Medium, TicketChannel.Email, customerId, deptId);
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);

        _tickets.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _customers.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _surveys.Setup(r => r.ExistsForTicketAsync(ticketId, default)).ReturnsAsync(false);

        await _job.ExecuteAsync(ticketId, agentId, deptId);

        _surveys.Verify(r => r.AddAsync(It.IsAny<CsatSurvey>(), default), Times.Once);
        _mediator.Verify(m => m.Send(
            It.Is<CreateNotificationCommand>(c =>
                c.Type == Domain.Notifications.NotificationType.SurveyAvailable),
            default), Times.Once);
    }

    [Fact]
    public async Task Execute_ExistingSurvey_SkipsCreation()
    {
        var ticketId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create(
            customerId, "Issue", "مشكلة", "Description", "وصف",
            TicketPriority.Medium, TicketChannel.Email, customerId, deptId);
        _tickets.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _surveys.Setup(r => r.ExistsForTicketAsync(ticketId, default)).ReturnsAsync(true);

        await _job.ExecuteAsync(ticketId, Guid.NewGuid(), deptId);

        _surveys.Verify(r => r.AddAsync(It.IsAny<CsatSurvey>(), default), Times.Never);
    }
}
