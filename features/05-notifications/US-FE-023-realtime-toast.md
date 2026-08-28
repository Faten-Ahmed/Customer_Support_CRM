# US-FE-023 — Real-Time Notification Toast & SignalR Manager

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
**As a** user, **I want to** see a toast pop-up when a new notification arrives, **so that** I notice urgent alerts even when I'm on a different page.

## Acceptance Criteria
- [ ] New `ReceiveNotification` push → toast appears bottom-right with notification title + body (3s auto-dismiss, or persistent for SlaBreached/Critical)
- [ ] Toast has "View" link that navigates to the related entity
- [ ] Multiple toasts stack (max 3 visible at once; older pushed off)
- [ ] SignalR connection established on login; reconnects automatically on disconnect (exponential back-off, max 30s)
- [ ] Connection status indicator in header (green dot = connected, grey = reconnecting)

## Technical Notes
- Component: `NotificationToastComponent`
- Service: `SignalRService` (manages `NotificationHub`, `DashboardHub`, `ChatHub` connections)
- SignalR: `@microsoft/signalr` npm package
- Spec: `specs/api/notifications.md`

## Dependencies
- US-BE-054, US-FE-022
