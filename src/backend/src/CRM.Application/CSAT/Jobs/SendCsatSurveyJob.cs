using CRM.Application.Notifications.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Notifications;
using CRM.Domain.Surveys;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.CSAT.Jobs;

public class SendCsatSurveyJob
{
    private readonly ICsatSurveyRepository _surveys;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;
    private readonly IMediator _mediator;

    public SendCsatSurveyJob(
        ICsatSurveyRepository surveys,
        ITicketRepository tickets,
        ICustomerRepository customers,
        IMediator mediator)
    {
        _surveys = surveys;
        _tickets = tickets;
        _customers = customers;
        _mediator = mediator;
    }

    public async Task ExecuteAsync(Guid ticketId, Guid agentId, Guid departmentId)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found.");

        if (await _surveys.ExistsForTicketAsync(ticketId)) return;

        var customer = await _customers.FindByIdAsync(ticket.CustomerId)
            ?? throw new Exception($"Customer {ticket.CustomerId} not found.");

        var survey = CsatSurvey.Create(
            ticketId, customer.Id, agentId, departmentId,
            ticket.TicketNumber, ticket.Subject);

        await _surveys.AddAsync(survey);
        await _surveys.SaveChangesAsync();

        await _mediator.Send(new CreateNotificationCommand(
            UserId: customer.Id,
            Type: NotificationType.SurveyAvailable,
            Title: "Rate your support experience",
            Body: $"Your ticket #{ticket.TicketNumber} was closed. Please rate your experience.",
            EntityType: "CsatSurvey",
            EntityId: survey.Id));
    }
}
