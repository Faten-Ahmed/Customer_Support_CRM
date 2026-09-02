# US-FE-022 — Notification Bell & Inbox

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


**Epic:** Agent Dashboard
**Roles:** Any authenticated user
**As a** user, **I want to** see a notification bell with unread count and an inbox, **so that** I never miss important alerts.

## Acceptance Criteria
- [ ] Bell icon in app header with red badge showing unread count (from `GET /notifications/unread-count`)
- [ ] Clicking bell opens slide-out panel with notification list (newest first)
- [ ] Notification item: icon per type, title, body (truncated), timestamp, unread highlight
- [ ] Click notification: marks as read + navigates to the referenced entity (ticket, article, etc.)
- [ ] "Mark all as read" button
- [ ] Filter: "Unread only" toggle
- [ ] "Load more" for older notifications

## Technical Notes
- Component: `NotificationBellComponent`, `NotificationInboxComponent`
- Services: `NotificationService.list()`, `NotificationService.markRead()`, `NotificationService.markAllRead()`, `NotificationService.getUnreadCount()`
- SignalR: subscribes to `ReceiveNotification` and `UnreadCountUpdated`

## Dependencies
- US-BE-055, US-BE-056, US-BE-057, US-FE-005
