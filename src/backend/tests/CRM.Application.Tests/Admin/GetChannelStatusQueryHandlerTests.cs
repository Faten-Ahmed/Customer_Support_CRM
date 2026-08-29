using CRM.Application.Admin.Channels.Queries;
using CRM.Domain.Channels;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class GetChannelStatusQueryHandlerTests
{
    private readonly Mock<IEmailHealthChecker> _email = new();
    private readonly Mock<ITwilioHealthChecker> _twilio = new();
    private readonly Mock<ILiveChatSessionRepository> _liveChat = new();
    private readonly GetChannelStatusQueryHandler _handler;

    public GetChannelStatusQueryHandlerTests()
    {
        _handler = new GetChannelStatusQueryHandler(
            _email.Object, _twilio.Object, _liveChat.Object);
    }

    [Fact]
    public async Task Handle_AllChannelsHealthy_Returns5Channels()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, DateTime.UtcNow.AddHours(-1), null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(3, 1));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        Assert.Equal(5, result.Channels.Count);
        Assert.Contains(result.Channels, c => c.Channel == "email" && c.Configured);
        Assert.Contains(result.Channels, c => c.Channel == "portal" && c.Configured);
    }

    [Fact]
    public async Task Handle_SmtpDown_ReturnsEmailDisconnected()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(false, null, "Connection refused"));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(0, 0));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var email = result.Channels.Single(c => c.Channel == "email");
        Assert.False(email.Connected);
        Assert.Equal("Connection refused", email.Error);
    }

    [Fact]
    public async Task Handle_TwilioInvalid_ReturnsWhatsAppAndSmsDisconnected()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, null, null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(false, "Invalid credentials"));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(0, 0));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var wa = result.Channels.Single(c => c.Channel == "whatsapp");
        var sms = result.Channels.Single(c => c.Channel == "sms");
        Assert.False(wa.Connected);
        Assert.False(sms.Connected);
    }

    [Fact]
    public async Task Handle_LiveChat_ReturnsSessionCounts()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, null, null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(ActiveSessions: 5, PendingHandoffs: 2));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var chat = result.Channels.Single(c => c.Channel == "liveChat");
        Assert.Equal(5, chat.ActiveSessions);
        Assert.Equal(2, chat.PendingHandoffs);
    }
}
