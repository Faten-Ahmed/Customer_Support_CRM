# Feature Spec — Notifications

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


> Requirements: `REQ-NOT-*`
> API: `specs/api/notifications.md`
> Domain entities: `Notification`
> Real-time: SignalR `NotificationHub`

---

## Overview

The notification system delivers in-app alerts to all user roles (Agents, Managers, Admins, Customers). Notifications are persisted in the database for inbox access and pushed in real time via SignalR. No email or SMS notifications are sent for in-app alerts — those are tracked as separate outbound channel events.

---

## Notification Types

| Type | Recipient(s) | Trigger |
|------|-------------|---------|
| `TicketAssigned` | Agent | Ticket assigned to them |
| `TicketReopened` | Agent | Customer reopens a resolved ticket |
| `NewMessage` | Agent | New customer message on their ticket |
| `NewInternalNote` | Agent, Manager | Internal note posted on a ticket they're watching |
| `SlaWarning` | Agent | SLA clock at Warning threshold (80%) |
| `SlaBreached` | Agent, Manager | SLA clock at Breach threshold (100%) |
| `SlaCriticalBreach` | Agent, Manager, Admin | SLA clock at CriticalBreach (200%) |
| `TicketEscalated` | Manager, Admin | Ticket escalated in their department |
| `UnassignedTicketAlert` | Manager | No agent available to auto-assign |
| `KbArticleSubmittedForReview` | Manager | New KB article awaiting review |
| `KbArticleRejected` | Agent (author) | Their KB article was rejected |
| `KbArticlePublished` | Agent (author) | Their KB article was published |
| `TicketReplyReceived` | Customer | Agent replied to their ticket |
| `TicketStatusChanged` | Customer | Their ticket status changed |
| `TicketClosed` | Customer | Their ticket was closed |
| `SurveyAvailable` | Customer | CSAT survey sent to them |

---

## Business Rules

**BR-NOT-001** Each `Notification` record is scoped to a single user (`UserId`). There are no "group" notifications in the database — if 3 agents need to be notified, 3 records are created.

**BR-NOT-002** Notifications are never deleted. They are marked read (`IsRead = true`, `ReadAt = timestamp`) via `PUT /notifications/{id}/read` or `PUT /notifications/read-all`.

**BR-NOT-003** A user can only read/mark their own notifications. Attempting to access another user's notification ID returns `403`.

**BR-NOT-004** `GET /notifications/unread-count` must be fast — backed by a cached count (Redis, TTL 60s) per user, invalidated on new notification or read-all.

**BR-NOT-005** Notification content is in the language of the recipient's profile preference (English or Arabic). If `ContentAr` is not provided for a notification template, fall back to English.

**BR-NOT-006** Duplicate suppression: for SLA notifications, check that a notification of the same `type` for the same `entityId` does not already exist before inserting. This prevents duplicate SLA alerts from the monitoring job running multiple times.

**BR-NOT-007** Maximum notification age shown in UI: all notifications are stored permanently, but the default `GET /notifications` view returns only those within the last 90 days. Pass `?all=true` (Admin only) to get older ones.

---

## Real-Time Delivery (SignalR NotificationHub)

**Connection:**
- Hub URL: `ws://localhost:5000/hubs/notifications?access_token=<token>`
- Clients authenticate with their Bearer JWT as a query parameter.
- On connect, the server adds the connection to a user-specific group `user-{userId}`.
- On disconnect, the connection is removed from the group automatically.

**Server → Client methods:**

| Method | Payload | When |
|--------|---------|------|
| `ReceiveNotification` | Full notification object | A new notification is created for this user |
| `UnreadCountUpdated` | `{ "count": N }` | After any notification read/create event |

**Guaranteed delivery gap:** SignalR is best-effort (no message queue backing). If the user is offline when a notification is created, the database record exists and will be fetched via `GET /notifications` on next login. The SignalR push is fire-and-forget.

---

## Notification Creation Flow

1. Domain event published (e.g., `TicketAssigned`).
2. Application service `NotificationService.CreateAsync(type, recipientId, entityType, entityId, title, body)` is called.
3. Check duplicate suppression (BR-NOT-006) for SLA types.
4. Persist `Notification` record.
5. Invalidate Redis unread-count cache for recipient.
6. Push via SignalR: `NotificationHubContext.Clients.Group("user-{recipientId}").SendAsync("ReceiveNotification", payload)`.
7. Push updated count: `SendAsync("UnreadCountUpdated", { count: newCount })`.

---

## Notification Body Templates

Notification bodies use simple token substitution:

| Token | Replaced With |
|-------|---------------|
| `{{ticketNumber}}` | `TKT-2025-00043` |
| `{{subject}}` | Ticket subject |
| `{{agentName}}` | Agent full name |
| `{{customerName}}` | Customer full name |
| `{{slaPercent}}` | e.g., `80%` |
| `{{minutesRemaining}}` | Remaining SLA minutes |

Example — `SlaWarning`:
```
Title: "SLA Warning — {{ticketNumber}}"
Body:  "Ticket {{ticketNumber}} is at {{slaPercent}} of its resolution SLA. {{minutesRemaining}} minutes remaining."
```

---

## Acceptance Criteria

**AC-NOT-001** Given a ticket is assigned to Agent A, then a `TicketAssigned` notification is created for Agent A and pushed via SignalR within 2 seconds of the assignment.

**AC-NOT-002** Given a user calls `PUT /notifications/{id}/read` where the notification belongs to a different user, then the response is `403`.

**AC-NOT-003** Given 5 unread notifications exist, when `PUT /notifications/read-all` is called, then all 5 are marked read, `markedRead = 5` is returned, and the Redis cache for this user's unread count is invalidated.

**AC-NOT-004** Given the SlaMonitorJob runs twice in 5 minutes and the ticket is still at 80%, then only one `SlaWarning` notification exists for that ticket (duplicate suppressed).

**AC-NOT-005** Given a Customer's ticket receives an agent reply, then a `TicketReplyReceived` notification is created for the Customer (not the agent).

**AC-NOT-006** Given `GET /notifications/unread-count` is called, then the response time is under 100ms (served from cache after first call).

**AC-NOT-007** Given a user has 3 notifications older than 90 days and 2 within 90 days, when `GET /notifications` is called without `?all=true`, then only 2 notifications are returned.

---

## Edge Cases

- **User offline**: notification is persisted. When user reconnects and client calls `GET /notifications`, they see all missed notifications.
- **Bulk ticket updates (admin action)**: if an admin bulk-updates 100 tickets at once, 100 notifications should not spam the agent. Implement a 5-second debounce: batch events in the same second into a single summary notification (e.g., "5 tickets assigned to you") — to be designed in implementation.
- **Customer with no portal account**: a Customer created by an agent without portal registration cannot receive portal notifications. Notifications are stored but the customer has no way to see them until they register. No external push channel in v1.

---

## Integration Points

The notification system is a pure consumer — it reacts to domain events from all other modules. It publishes no events of its own.

| Domain Event Consumed | Notification Type Created |
|----------------------|--------------------------|
| `TicketAssigned` | `TicketAssigned` |
| `TicketReopened` | `TicketReopened` |
| `TicketMessageAdded` (IsInternal=false, sender=Agent) | `TicketReplyReceived` (→ Customer) |
| `TicketMessageAdded` (IsInternal=false, sender=Customer) | `NewMessage` (→ Agent) |
| `TicketMessageAdded` (IsInternal=true) | `NewInternalNote` (→ Agent watchers) |
| `SlaWarningTriggered` | `SlaWarning` |
| `SlaBreached` | `SlaBreached` |
| `SlaCriticalBreachTriggered` | `SlaCriticalBreach` |
| `TicketEscalated` | `TicketEscalated` |
| `UnassignedTicketAlert` | `UnassignedTicketAlert` |
| `KbArticleSubmittedForReview` | `KbArticleSubmittedForReview` |
| `KbArticleRejected` | `KbArticleRejected` |
| `KbArticlePublished` | `KbArticlePublished` |
| `TicketStatusChanged` | `TicketStatusChanged` (→ Customer) |
| `TicketClosed` | `TicketClosed` (→ Customer) |
| `CsatSurveySent` | `SurveyAvailable` (→ Customer) |
