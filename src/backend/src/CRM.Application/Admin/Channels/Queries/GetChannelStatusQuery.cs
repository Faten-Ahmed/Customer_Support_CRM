using CRM.Application.Admin.Channels.DTOs;
using CRM.Domain.Channels;
using MediatR;

namespace CRM.Application.Admin.Channels.Queries;

public record GetChannelStatusQuery : IRequest<ChannelStatusListDto>;

public class GetChannelStatusQueryHandler
    : IRequestHandler<GetChannelStatusQuery, ChannelStatusListDto>
{
    private readonly IEmailHealthChecker _email;
    private readonly ITwilioHealthChecker _twilio;
    private readonly ILiveChatSessionRepository _liveChat;

    public GetChannelStatusQueryHandler(
        IEmailHealthChecker email,
        ITwilioHealthChecker twilio,
        ILiveChatSessionRepository liveChat)
    {
        _email = email;
        _twilio = twilio;
        _liveChat = liveChat;
    }

    public async Task<ChannelStatusListDto> Handle(
        GetChannelStatusQuery query, CancellationToken ct)
    {
        var emailTask = _email.CheckAsync(ct);
        var twilioTask = _twilio.CheckAsync(ct);
        var liveChatTask = _liveChat.GetStatsAsync(ct);

        await Task.WhenAll(emailTask, twilioTask, liveChatTask);

        var emailResult = emailTask.Result;
        var twilioResult = twilioTask.Result;
        var liveChatStats = liveChatTask.Result;

        var channels = new List<ChannelStatusDto>
        {
            new("email", true, emailResult.Connected,
                emailResult.LastMessageAt, null, null, emailResult.Error),

            new("whatsapp", true, twilioResult.Valid,
                null, null, null, twilioResult.Valid ? null : twilioResult.Error),

            new("sms", true, twilioResult.Valid,
                null, null, null, twilioResult.Valid ? null : twilioResult.Error),

            new("liveChat", true, true,
                null, liveChatStats.ActiveSessions, liveChatStats.PendingHandoffs, null),

            new("portal", true, true, null, null, null, null)
        };

        return new ChannelStatusListDto(channels);
    }
}
