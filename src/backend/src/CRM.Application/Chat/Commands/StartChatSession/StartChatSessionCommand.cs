using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Chat.Commands.StartChatSession;

public record StartChatSessionCommand(
    Guid CustomerId,
    string CustomerName,
    Guid? DepartmentId) : IRequest<Guid>;

public class StartChatSessionCommandHandler : IRequestHandler<StartChatSessionCommand, Guid>
{
    private readonly IChatSessionRepository _repo;
    private readonly ITicketRepository _tickets;

    public StartChatSessionCommandHandler(IChatSessionRepository repo, ITicketRepository tickets)
    {
        _repo = repo;
        _tickets = tickets;
    }

    public async Task<Guid> Handle(StartChatSessionCommand req, CancellationToken ct)
    {
        var session = ChatSession.Create(req.CustomerId, req.CustomerName, req.DepartmentId);

        var subject = $"Live Chat – {req.CustomerName}";
        var ticket = Ticket.Create(
            customerId: req.CustomerId,
            subject: subject,
            subjectAr: subject,
            description: "Live chat session",
            descriptionAr: "جلسة دردشة مباشرة",
            priority: TicketPriority.Medium,
            channel: TicketChannel.LiveChat,
            createdByUserId: req.CustomerId,
            departmentId: req.DepartmentId);

        session.SetLinkedTicketId(ticket.Id);

        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);

        await _repo.AddAsync(session, ct);
        await _repo.SaveAsync(ct);

        return session.Id;
    }
}
