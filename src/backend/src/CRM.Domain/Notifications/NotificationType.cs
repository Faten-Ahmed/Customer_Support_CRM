namespace CRM.Domain.Notifications;

public enum NotificationType
{
    TicketAssigned,
    TicketReopened,
    NewMessage,
    NewInternalNote,
    SlaWarning,
    SlaBreached,
    SlaCriticalBreach,
    TicketEscalated,
    UnassignedTicketAlert,
    KbArticleSubmittedForReview,
    KbArticleRejected,
    KbArticlePublished,
    TicketReplyReceived,
    TicketStatusChanged,
    TicketClosed,
    SurveyAvailable
}
