# US-BE-054 — Push Notification via SignalR (NotificationHub)

## Technology Requirements

**Frontend (if applicable):**
- **UI Framework:** Angular Material ONLY (NOT Tailwind, NOT Bootstrap)
- **RTL/LTR:** Must support both RTL (Arabic) and LTR (English)
- **Arabic:** All user-facing text must be translatable to Arabic
- **English:** All user-facing text must be in English
- **i18n:** Use Angular's built-in i18n (`@angular/localize`)

**Backend (if applicable):**
- **Framework:** .NET 10, C#
- **API:** RESTful with OpenAPI
- **Language:** C# with Arabic/English string resources

---


**Epic:** Notifications
**Roles:** System → Client
**As a** logged-in user, **I want to** receive notifications instantly without refreshing, **so that** I can react to ticket events in real time.

## Acceptance Criteria
- [ ] `NotificationHub` authenticates via JWT query param `?access_token=<token>`
- [ ] On connect: server adds connection to `user-{userId}` SignalR group
- [ ] After `NotificationService.CreateAsync` persists a notification: `IHubContext<NotificationHub>.Clients.Group("user-{recipientId}").SendAsync("ReceiveNotification", notification)`
- [ ] Updated unread count is pushed immediately after: `SendAsync("UnreadCountUpdated", { count: N })`
- [ ] If user is offline: notification is persisted in DB; SignalR push is fire-and-forget (no error on failed push)

## Technical Notes
- Hub URL: `ws://localhost:5000/hubs/notifications`
- Implementation: `NotificationHub : Hub`, `INotificationPusher` service wrapping `IHubContext<NotificationHub>`
- Business rule: BR-NOT-002, guaranteed delivery gap per `specs/features/05-notifications.md`

## Dependencies
- US-BE-053
