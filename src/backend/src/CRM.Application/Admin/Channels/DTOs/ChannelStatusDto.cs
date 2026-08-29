namespace CRM.Application.Admin.Channels.DTOs;

public record ChannelStatusDto(
    string Channel,
    bool Configured,
    bool Connected,
    DateTime? LastMessageAt,
    int? ActiveSessions,
    int? PendingHandoffs,
    string? Error);

public record ChannelStatusListDto(IReadOnlyList<ChannelStatusDto> Channels);
