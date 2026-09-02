using CRM.Application.Chat.DTOs;
using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Chat.Commands.SendChatMessage;

public record SendChatMessageCommand(
    Guid SessionId,
    string SenderRole,
    Guid? SenderId,
    string Body) : IRequest<ChatMessageDto>;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    private readonly IChatSessionRepository _repo;
    private readonly ITicketMessageRepository _ticketMessages;

    public SendChatMessageCommandHandler(
        IChatSessionRepository repo,
        ITicketMessageRepository ticketMessages)
    {
        _repo = repo;
        _ticketMessages = ticketMessages;
    }

    public async Task<ChatMessageDto> Handle(SendChatMessageCommand req, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(req.SessionId, ct)
            ?? throw new InvalidOperationException($"Chat session {req.SessionId} not found.");

        if (session.Status == ChatSessionStatus.Closed)
            throw new InvalidOperationException("Cannot send a message to a closed session.");

        var msg = session.AddMessage(req.SenderRole, req.SenderId, req.Body);
        await _repo.AddMessageAsync(msg, ct);
        await _repo.SaveAsync(ct);

        if (session.LinkedTicketId.HasValue)
        {
            var ticketMsg = TicketMessage.Create(
                session.LinkedTicketId.Value,
                req.Body,
                isInternal: false,
                authorUserId: req.SenderRole == "Agent" ? req.SenderId : null,
                authorCustomerId: req.SenderRole == "Customer" ? req.SenderId : null);
            await _ticketMessages.AddAsync(ticketMsg, ct);
            await _ticketMessages.SaveChangesAsync(ct);
        }

        return new ChatMessageDto(msg.Id, msg.SessionId, msg.SenderRole, msg.SenderId, msg.Body, msg.SentAt);
    }
}
